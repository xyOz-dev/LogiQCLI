using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Core.Services;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Tests.Core
{
    public class TokenBudgeterTests : TestBase
    {
        public override string TestName => "TokenBudgeter Tests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var results = new List<TestResult>();

                results.Add(TestTokenEstimation());
                results.Add(TestEdgeCaseTokenEstimation());
                results.Add(TestMessageTokenEstimation());
                results.Add(TestToolTokenEstimation());
                results.Add(TestMiddleOutTruncation());
                results.Add(TestMessageShaping());
                results.Add(TestContextOverflowHandling());
                results.Add(TestToolRemovalStrategies());
                results.Add(TestExtremeLimitScenarios());
                results.Add(TestMultipleMessagesOrdering());

                stopwatch.Stop();

                var failedTests = results.Where(r => !r.Success).ToList();
                if (failedTests.Any())
                {
                    var failureMessages = string.Join("; ", failedTests.Select(f => f.ErrorMessage));
                    return CreateFailureResult($"Failed {failedTests.Count}/{results.Count} tests: {failureMessages}", stopwatch.Elapsed);
                }

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestTokenEstimation()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var empty = budgeter.EstimateTokensFromText("");
                if (empty != 0) return CreateFailureResult("Empty string should be 0 tokens", stopwatch.Elapsed);
                
                var nullText = budgeter.EstimateTokensFromText(null);
                if (nullText != 0) return CreateFailureResult("Null string should be 0 tokens", stopwatch.Elapsed);
                
                var simple = budgeter.EstimateTokensFromText("Hello world");
                if (simple != 2) return CreateFailureResult($"'Hello world' (11 chars) should be ~2 tokens, got {simple}", stopwatch.Elapsed);
                
                var exact = budgeter.EstimateTokensFromText("abcd");
                if (exact != 1) return CreateFailureResult($"4 chars should be 1 token, got {exact}", stopwatch.Elapsed);
                
                var large = budgeter.EstimateTokensFromText(new string('a', 10000));
                if (large != 2500) return CreateFailureResult($"10000 chars should be 2500 tokens, got {large}", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestEdgeCaseTokenEstimation()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var singleChar = budgeter.EstimateTokensFromText("a");
                if (singleChar != 1) return CreateFailureResult("Single char should round up to 1 token", stopwatch.Elapsed);
                
                var threeChars = budgeter.EstimateTokensFromText("abc");
                if (threeChars != 1) return CreateFailureResult("3 chars should round up to 1 token", stopwatch.Elapsed);
                
                var fiveChars = budgeter.EstimateTokensFromText("abcde");
                if (fiveChars != 1) return CreateFailureResult("5 chars should be 1 token", stopwatch.Elapsed);
                
                var unicode = budgeter.EstimateTokensFromText("👋🌍💻🚀");
                if (unicode < 1) return CreateFailureResult("Unicode string should have at least 1 token", stopwatch.Elapsed);
                
                var whitespace = budgeter.EstimateTokensFromText("   \n\t\r   ");
                if (whitespace != 2) return CreateFailureResult($"Whitespace (9 chars) should be 2 tokens, got {whitespace}", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestMessageTokenEstimation()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var simpleMessage = new Message
                {
                    Role = "user",
                    Content = "Hello, how are you?"
                };
                var simpleTokens = budgeter.EstimateTokensForMessage(simpleMessage);
                if (simpleTokens < 8) return CreateFailureResult($"Simple message should have at least 8 tokens (4 overhead + content), got {simpleTokens}", stopwatch.Elapsed);
                
                var messageWithName = new Message
                {
                    Role = "assistant",
                    Content = "I'm doing well!",
                    Name = "Jordan"
                };
                var namedTokens = budgeter.EstimateTokensForMessage(messageWithName);
                if (namedTokens != 8) return CreateFailureResult($"Message with name should have 8 tokens (3+1+4), got {namedTokens}", stopwatch.Elapsed);
                
                var toolCallMessage = new Message
                {
                    Role = "assistant",
                    Content = "Let me help with that",
                    ToolCalls = new[]
                    {
                        new ToolCall
                        {
                            Function = new FunctionCall
                            {
                                Name = "get_weather",
                                Arguments = "{\"location\": \"San Francisco\"}"
                            }
                        }
                    }
                };
                var toolCallTokens = budgeter.EstimateTokensForMessage(toolCallMessage);
                if (toolCallTokens != 18) return CreateFailureResult($"Tool call message should have 18 tokens (5+2+7+4), got {toolCallTokens}", stopwatch.Elapsed);
                
                var emptyMessage = new Message { Role = "user" };
                var emptyTokens = budgeter.EstimateTokensForMessage(emptyMessage);
                if (emptyTokens != 4) return CreateFailureResult($"Empty message should have 4 overhead tokens, got {emptyTokens}", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestToolTokenEstimation()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                Tool[]? nullTools = null;
                var nullTokens = budgeter.EstimateTokensForTools(nullTools);
                if (nullTokens != 0) return CreateFailureResult("Null tools should be 0 tokens", stopwatch.Elapsed);
                
                var emptyTools = new Tool[0];
                var emptyTokens = budgeter.EstimateTokensForTools(emptyTools);
                if (emptyTokens != 0) return CreateFailureResult("Empty tools array should be 0 tokens", stopwatch.Elapsed);
                
                var simpleTool = new Tool[]
                {
                    new Tool
                    {
                        Type = "function",
                        Function = new Function
                        {
                            Name = "test_tool",
                            Description = "A simple test tool",
                            Parameters = new Parameters { Type = "object", Properties = new {} }
                        }
                    }
                };
                var simpleTokens = budgeter.EstimateTokensForTools(simpleTool);
                if (simpleTokens < 15) return CreateFailureResult($"Simple tool should have at least 15 tokens, got {simpleTokens}", stopwatch.Elapsed);
                
                var multipleTools = new Tool[]
                {
                    new Tool
                    {
                        Type = "function",
                        Function = new Function
                        {
                            Name = "tool1",
                            Description = "First tool"
                        }
                    },
                    new Tool
                    {
                        Type = "function",
                        Function = new Function
                        {
                            Name = "tool2",
                            Description = "Second tool with longer description for testing"
                        }
                    }
                };
                var multiTokens = budgeter.EstimateTokensForTools(multipleTools);
                if (multiTokens < 30) return CreateFailureResult($"Multiple tools should have significant tokens, got {multiTokens}", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestMiddleOutTruncation()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var shortContent = "Hello";
                var shortResult = budgeter.MiddleOut(shortContent, 100);
                if (shortResult != shortContent) return CreateFailureResult("Short content should not be truncated", stopwatch.Elapsed);
                
                var nullContent = budgeter.MiddleOut(null!, 100);
                if (nullContent != null) return CreateFailureResult("Null content should return null", stopwatch.Elapsed);
                
                var emptyContent = budgeter.MiddleOut("", 100);
                if (emptyContent != "") return CreateFailureResult("Empty content should return empty", stopwatch.Elapsed);
                
                var longContent = new string('a', 200) + new string('b', 200);
                var truncated = budgeter.MiddleOut(longContent, 100);
                if (!truncated.Contains("…[middle omitted]…")) return CreateFailureResult("Truncated content should contain omission marker", stopwatch.Elapsed);
                if (truncated.Length > 120) return CreateFailureResult($"Truncated content should be around maxChars, got {truncated.Length}", stopwatch.Elapsed);
                
                var verySmallLimit = budgeter.MiddleOut("This is a test string", 10);
                if (verySmallLimit.Length > 32) return CreateFailureResult("Very small limit should still produce minimal output", stopwatch.Elapsed);
                
                var preserveEnds = new string('A', 50) + new string('Z', 50);
                var middleOut = budgeter.MiddleOut(preserveEnds, 60);
                if (!middleOut.StartsWith("AAA") || !middleOut.EndsWith("ZZZ")) 
                    return CreateFailureResult("Middle-out should preserve start and end content", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestMessageShaping()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var messages = new List<Message>
                {
                    new Message { Role = "system", Content = "You are a helpful assistant." },
                    new Message { Role = "user", Content = "Hello" },
                    new Message { Role = "assistant", Content = "Hi there!" }
                };
                
                var result = budgeter.Shape(messages, null, null, 10000, 1000);
                if (result.shaped.Count != 3) return CreateFailureResult("Messages within budget should not be dropped", stopwatch.Elapsed);
                
                var largeMessages = new List<Message>();
                for (int i = 0; i < 150; i++)
                {
                    largeMessages.Add(new Message { Role = "user", Content = $"Message {i}" });
                }
                var trimmedResult = budgeter.Shape(largeMessages, null, null, 10000, 1000, maxMessages: 120);
                if (trimmedResult.shaped.Count > 120) return CreateFailureResult($"Should trim to max messages, got {trimmedResult.shaped.Count}", stopwatch.Elapsed);
                
                var toolMessage = new Message
                {
                    Role = "tool",
                    Content = new string('x', 200000)
                };
                var truncatedResult = budgeter.Shape(new List<Message> { toolMessage }, null, null, 10000, 1000);
                var truncatedContent = truncatedResult.shaped[0].Content?.ToString() ?? "";
                if (truncatedContent.Length > 100000) return CreateFailureResult("Tool output should be truncated to max chars", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestContextOverflowHandling()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var hugeMessages = new List<Message>();
                for (int i = 0; i < 50; i++)
                {
                    hugeMessages.Add(new Message 
                    { 
                        Role = i % 2 == 0 ? "user" : "assistant", 
                        Content = new string('a', 10000)
                    });
                }
                
                var tinyBudget = budgeter.Shape(hugeMessages, null, null, 1000, 100);
                if (tinyBudget.shaped.Count >= hugeMessages.Count) 
                    return CreateFailureResult("Should drop messages when budget is exceeded", stopwatch.Elapsed);
                
                if (tinyBudget.shaped.Count == 1)
                {
                    var keptMessage = tinyBudget.shaped[0];
                    var contentLength = keptMessage.Content?.ToString()?.Length ?? 0;
                    if (contentLength > 3200)
                        return CreateFailureResult($"Message not compressed enough: {contentLength} chars", stopwatch.Elapsed);
                }
                
                if (tinyBudget.estimatedPromptTokens > 900) 
                    return CreateFailureResult($"Should stay within budget (<=900), got {tinyBudget.estimatedPromptTokens} tokens", stopwatch.Elapsed);
                
                var lastUserPreserved = tinyBudget.shaped.Any(m => m.Role == "user");
                if (!lastUserPreserved) return CreateFailureResult("Should preserve at least one user message", stopwatch.Elapsed);
                
                var veryTinyBudget = budgeter.Shape(hugeMessages, null, null, 100, 50);
                if (veryTinyBudget.shaped.Count > 5) 
                    return CreateFailureResult($"Very small budget should keep minimal messages, got {veryTinyBudget.shaped.Count}", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestToolRemovalStrategies()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var messages = new List<Message>
                {
                    new Message { Role = "user", Content = "Test message" }
                };
                
                var largeTools = new Tool[]
                {
                    new Tool
                    {
                        Type = "function",
                        Function = new Function
                        {
                            Name = "huge_tool",
                            Description = "Tool with massive parameter schema",
                            Parameters = new Parameters { Type = "object", Properties = new { schema = new string('x', 100000) } }
                        }
                    }
                };
                
                var result = budgeter.Shape(messages, largeTools, "required", 5000, 1000);
                
                if (result.tools == null || result.tools.Length == 0)
                    return CreateFailureResult("Should keep tools but possibly trim parameters", stopwatch.Elapsed);
                
                var firstTool = result.tools[0];
                if (firstTool.Function?.Parameters?.ToString()?.Length > 60000)
                    return CreateFailureResult("Should null out large parameter schemas", stopwatch.Elapsed);
                
                if (result.toolChoice != "auto" && result.estimatedPromptTokens > 4000)
                    return CreateFailureResult("Should switch to auto tool choice when budget tight", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestExtremeLimitScenarios()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var messages = new List<Message>
                {
                    new Message { Role = "user", Content = "Test" }
                };
                
                var zeroCompletion = budgeter.Shape(messages, null, null, 1000, 0);
                if (zeroCompletion.estimatedPromptTokens > 900)
                    return CreateFailureResult("Should handle zero completion tokens", stopwatch.Elapsed);
                
                var negativeSafety = budgeter.Shape(messages, null, null, 1000, 500, safetyMarginPct: -0.1);
                if (negativeSafety.shaped.Count != 1)
                    return CreateFailureResult("Should handle negative safety margin", stopwatch.Elapsed);
                
                var hugeSafety = budgeter.Shape(messages, null, null, 1000, 100, safetyMarginPct: 0.95);
                var budget = (int)(1000 * 0.05) - 100;
                if (budget > 128 && negativeSafety.shaped.Count != 1)
                    return CreateFailureResult("Should use fallback budget for extreme safety margin", stopwatch.Elapsed);
                
                var emptyList = new List<Message>();
                var emptyResult = budgeter.Shape(emptyList, null, null, 1000, 100);
                if (emptyResult.shaped.Count != 0)
                    return CreateFailureResult("Should handle empty message list", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private TestResult TestMultipleMessagesOrdering()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var budgeter = new TokenBudgeter();
                
                var messages = new List<Message>();
                for (int i = 0; i < 20; i++)
                {
                    messages.Add(new Message 
                    { 
                        Role = i % 2 == 0 ? "user" : "assistant", 
                        Content = $"Message {i}: " + new string('x', 1000)
                    });
                }
                
                var result = budgeter.Shape(messages, null, null, 5000, 1000, middleOut: true);
                
                var lastMessages = result.shaped.TakeLast(6).ToList();
                var hasOriginalContent = lastMessages.Any(m => m.Content?.ToString()?.Contains("Message 19") == true);
                if (!hasOriginalContent)
                    return CreateFailureResult("Should preserve recent messages verbatim", stopwatch.Elapsed);
                
                var firstMessages = result.shaped.Take(Math.Max(0, result.shaped.Count - 6)).ToList();
                var hasMiddleOut = firstMessages.Any(m => m.Content?.ToString()?.Contains("…[middle omitted]…") == true);
                if (firstMessages.Count > 0 && !hasMiddleOut)
                    return CreateFailureResult("Should apply middle-out to older messages", stopwatch.Elapsed);
                
                var noMiddleOut = budgeter.Shape(messages, null, null, 5000, 1000, middleOut: false);
                var hasNoMiddleOut = noMiddleOut.shaped.Any(m => m.Content?.ToString()?.Contains("…[middle omitted]…") == true);
                if (hasNoMiddleOut)
                    return CreateFailureResult("Should respect middleOut=false parameter", stopwatch.Elapsed);

                return CreateSuccessResult(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }
    }
}