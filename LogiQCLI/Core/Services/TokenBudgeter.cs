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
    // Lightweight token budgeting with conservative heuristics.
    //  - 4 chars ~= 1 token heuristic
    //  - Shapes ChatRequest to fit within endpoint context
    public class TokenBudgeter
    {
        private const int CharsPerToken = 4; // conservative

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
            return total + 4; // structural overhead
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
            // Ensure last user message preserved
            var messages = original.ToList();
            // Trim to max messages first (keep tail)
            if (messages.Count > maxMessages)
            {
                messages = messages.Skip(messages.Count - maxMessages).ToList();
            }

            // Truncate extremely large tool outputs to cap char length
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

            var budget = (int)Math.Floor(endpointContextLength * (1.0 - safetyMarginPct)) - targetCompletionTokens;
            if (budget < 256) budget = Math.Max(128, endpointContextLength / 4); // fallback guard

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

            // Reduce tool definitions first by switching toolChoice to auto or none if needed
            ToolDef[]? shapedTools = tools;
            string? shapedToolChoice = toolChoice;

            if (shapedTools != null && shapedTools.Length > 0)
            {
                // Try dropping parameter schemas (heavy) by nulling Parameters when ToString is big
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
                    shapedToolChoice = "auto"; // let model decide, may reduce bias
                }
            }

            // If still too large, compact older messages using middle-out/summarization
            // Strategy: always keep the last 1 user + last 1 assistant; then walk backwards summarizing older pairs
            int keepTail = Math.Min(6, messages.Count); // keep last few verbatim for coherence
            var head = messages.Take(messages.Count - keepTail).ToList();
            var tail = messages.Skip(messages.Count - keepTail).ToList();

            if (head.Count > 0)
            {
                for (int i = 0; i < head.Count; i++)
                {
                    var m = head[i];
                    var txt = ExtractText(m.Content);
                    if (string.IsNullOrEmpty(txt)) continue;
                    // aggressive middle-out to ~25% of original chars beyond 4k
                    var maxChars = Math.Min(4000, Math.Max(1000, txt.Length / 4));
                    head[i] = new Message
                    {
                        Role = m.Role,
                        Name = m.Name,
                        ToolCalls = m.ToolCalls,
                        ToolCallId = m.ToolCallId,
                        Content = MiddleOut(txt, maxChars)
                    };
                }
            }

            var shaped = head.Concat(tail).ToList();
            total = shaped.Sum(EstimateTokensForMessage) + EstimateTokensForTools(shapedTools);

            // If still exceeding, drop oldest messages progressively until fit, preserving last user
            int idx = 0;
            while (total > budget && shaped.Count > 1 && idx < 10_000)
            {
                shaped.RemoveAt(0);
                total = shaped.Sum(EstimateTokensForMessage) + EstimateTokensForTools(shapedTools);
                idx++;
            }

            return (shaped, shapedToolChoice, shapedTools, total);
        }

        public string MiddleOut(string content, int maxChars)
        {
            if (string.IsNullOrEmpty(content) || content.Length <= maxChars) return content;
            if (maxChars < 32) return content.Substring(0, Math.Min(32, content.Length));

            int keep = maxChars / 2 - 10;
            int tail = maxChars - keep - 20;
            if (keep <= 0 || tail <= 0) return content.Substring(0, maxChars);

            var head = content.Substring(0, keep);
            var end = content.Substring(content.Length - tail);
            return head + "\n…[middle omitted]…\n" + end;
        }
    }
}
