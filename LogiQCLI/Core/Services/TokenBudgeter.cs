using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using ToolDef = LogiQCLI.Tools.Core.Objects.Tool;
using FuncCall = LogiQCLI.Tools.Core.Objects.Function;

namespace LogiQCLI.Core.Services
{

    public class TokenBudgeter
    {
        private const int CharsPerToken = 4;

        public int EstimateTokensFromText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return Math.Max(1, text!.Length / CharsPerToken);
        }

        private static string ExtractText(object? content)
        {
            if (content == null) return string.Empty;
            return content switch
            {
                string s => s,
                _ => content.ToString() ?? string.Empty
            };
        }

        public int EstimateTokensForMessage(Message m)
        {
            var total = 0;
            if (m.Content != null) total += EstimateTokensFromText(ExtractText(m.Content));
            if (m.Name != null) total += EstimateTokensFromText(m.Name);
            if (m.ToolCalls != null && m.ToolCalls.Length > 0)
            {
                foreach (var tc in m.ToolCalls)
                {
                    total += EstimateTokensFromText(tc.Function?.Name);
                    total += EstimateTokensFromText(tc.Function?.Arguments);
                }
            }
            return total + 4;
        }

        public int EstimateTokensForTools(ToolDef[]? tools)
        {
            if (tools == null || tools.Length == 0) return 0;
            var sum = 0;
            foreach (var t in tools)
            {
                sum += EstimateTokensFromText(t.Function?.Name);
                sum += EstimateTokensFromText(t.Function?.Description);
                sum += EstimateTokensFromText(t.Function?.Parameters?.ToString());
            }
            return sum + tools.Length * 8;
        }

        public (IList<Message> shaped, string? toolChoice, ToolDef[]? tools, int estimatedPromptTokens) Shape(
            IList<Message> original,
            ToolDef[]? tools,
            string? toolChoice,
            int endpointContextLength,
            int targetCompletionTokens,
            double safetyMarginPct = 0.1,
            int maxMessages = 120,
            int maxToolOutputChars = 100_000,
            bool middleOut = true)
        {
            if (original == null || original.Count == 0)
                return (new List<Message>(), toolChoice, tools, 0);
            
            var messages = original.ToList();
            if (messages.Count > maxMessages)
            {
                messages = messages.Skip(messages.Count - maxMessages).ToList();
            }

            foreach (var m in messages)
            {
                if (m.Role != null && m.Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
                {
                    var text = ExtractText(m.Content);
                    if (!string.IsNullOrEmpty(text) && text.Length > maxToolOutputChars)
                    {
                        m.Content = middleOut ? MiddleOut(text, maxToolOutputChars) : text.Substring(0, maxToolOutputChars);
                    }
                }
            }

            var effectiveSafetyMargin = Math.Max(0, Math.Min(0.9, safetyMarginPct));
            var budget = (int)Math.Floor(endpointContextLength * (1.0 - effectiveSafetyMargin)) - targetCompletionTokens;
            if (budget < 256) budget = Math.Max(128, endpointContextLength / 4);

            if (messages.Count == 0) 
                return (messages, toolChoice, tools, 0);

            var estimator = new List<int>();
            var total = 0;
            foreach (var m in messages)
            {
                var t = EstimateTokensForMessage(m);
                estimator.Add(t);
                total += t;
            }
            total += EstimateTokensForTools(tools);

            if (total <= budget)
            {
                return (messages, toolChoice, tools, total);
            }
            
            if (total > budget * 3)
            {
                var lastUser = messages.LastOrDefault(m => m.Role == "user");
                if (lastUser != null)
                {
                    var content = ExtractText(lastUser.Content);
                    var maxChars = Math.Min(content.Length, Math.Max(50, (budget - 20) * 4)); // Reserve space for overhead
                    var compressedUser = new Message
                    {
                        Role = lastUser.Role,
                        Name = lastUser.Name,
                        Content = middleOut && content.Length > maxChars ? 
                            MiddleOut(content, maxChars) : 
                            content.Substring(0, Math.Min(content.Length, maxChars))
                    };
                    var compressedList = new List<Message> { compressedUser };
                    var compressedTotal = EstimateTokensForMessage(compressedUser) + EstimateTokensForTools(tools);
                    return (compressedList, toolChoice, tools, compressedTotal);
                }
            }

            ToolDef[]? shapedTools = tools;
            string? shapedToolChoice = toolChoice;

            if (shapedTools != null && shapedTools.Length > 0)
            {
                shapedTools = shapedTools.Select(t =>
                {
                    try
                    {
                        var pStr = t.Function?.Parameters?.ToString();
                        if (!string.IsNullOrEmpty(pStr) && pStr.Length > 60_000)
                        {
                            var clone = new ToolDef
                            {
                                Type = t.Type,
                                Function = new FuncCall
                                {
                                    Name = t.Function?.Name,
                                    Description = t.Function?.Description,
                                    Parameters = null
                                }
                            };
                            return clone;
                        }
                    }
                    catch { }
                    return t;
                }).ToArray();

                total = messages.Sum(EstimateTokensForMessage) + EstimateTokensForTools(shapedTools);
                if (total > budget)
                {
                    shapedToolChoice = "auto";
                }
            }

            var budgetPressure = (double)total / budget;
            int keepTail = budgetPressure > 10 ? 1 : (budgetPressure > 5 ? 2 : Math.Min(6, messages.Count));
            var head = messages.Take(Math.Max(0, messages.Count - keepTail)).ToList();
            var tail = messages.Skip(Math.Max(0, messages.Count - keepTail)).ToList();

            if (head.Count > 0 && total > budget)
            {
                for (int i = 0; i < head.Count; i++)
                {
                    var m = head[i];
                    var txt = ExtractText(m.Content);
                    if (string.IsNullOrEmpty(txt)) continue;
                    
                    var budgetRatio = (double)budget / total;
                    var compressionFactor = budgetRatio < 0.5 ? 0.1 : (budgetRatio < 0.7 ? 0.25 : 0.5);
                    var maxChars = Math.Min(4000, Math.Max(100, (int)(txt.Length * compressionFactor)));
                    
                    head[i] = new Message
                    {
                        Role = m.Role,
                        Name = m.Name,
                        ToolCalls = m.ToolCalls,
                        ToolCallId = m.ToolCallId,
                        Content = middleOut ? MiddleOut(txt, maxChars) : txt.Substring(0, Math.Min(txt.Length, maxChars))
                    };
                }
            }

            var shaped = head.Concat(tail).ToList();
            total = shaped.Sum(EstimateTokensForMessage) + EstimateTokensForTools(shapedTools);

            int idx = 0;
            while (total > budget && shaped.Count > 1 && idx < 10_000)
            {
                if (shaped.Count <= 1 || (shaped.Count == 2 && shaped[^1].Role == "user"))
                    break;
                    
                shaped.RemoveAt(0);
                total = shaped.Sum(EstimateTokensForMessage) + EstimateTokensForTools(shapedTools);
                idx++;
            }
            
            if (total > budget && shaped.Count > 1)
            {
                var lastUser = shaped.LastOrDefault(m => m.Role == "user");
                if (lastUser != null)
                {
                    var content = ExtractText(lastUser.Content);
                    if (!string.IsNullOrEmpty(content))
                    {
                        var maxTokensForMessage = budget - 10;
                        var maxChars = maxTokensForMessage * 4;
                        if (content.Length > maxChars)
                        {
                            lastUser = new Message
                            {
                                Role = lastUser.Role,
                                Name = lastUser.Name,
                                ToolCalls = lastUser.ToolCalls,
                                ToolCallId = lastUser.ToolCallId,
                                Content = middleOut ? MiddleOut(content, maxChars) : content.Substring(0, Math.Min(content.Length, maxChars))
                            };
                        }
                    }
                    shaped = new List<Message> { lastUser };
                    total = EstimateTokensForMessage(lastUser) + EstimateTokensForTools(shapedTools);
                }
            }

            return (shaped, shapedToolChoice, shapedTools, total);
        }

        public string MiddleOut(string content, int maxChars)
        {
            if (content == null) return null;
            if (string.IsNullOrEmpty(content) || content.Length <= maxChars) 
                return content;
            
            if (maxChars < 32)
                return content.Substring(0, Math.Min(maxChars, content.Length));

            const string marker = "\n…[middle omitted]…\n";
            var availableChars = maxChars - marker.Length;
            
            if (availableChars < 20)
                return content.Substring(0, Math.Min(maxChars, content.Length));

            int headChars = availableChars * 2 / 5;
            int tailChars = availableChars - headChars;
            
            var head = content.Substring(0, Math.Min(headChars, content.Length));
            var tailStart = Math.Max(headChars, content.Length - tailChars);
            var end = content.Substring(tailStart);
            
            return head + marker + end;
        }
    }
}
