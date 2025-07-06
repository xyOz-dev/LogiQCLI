using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Services;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;

namespace LogiQCLI.Tests.Infrastructure
{
    public class OpenRouterClientCacheTests : TestBase
    {
        public override string TestName => "OpenRouter Client Cache Tests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var results = new List<TestResult>();

                results.Add(await TestCacheStrategyNoneAsync());
                results.Add(await TestCacheStrategyAutoAsync());
                results.Add(await TestCacheStrategyAggressiveAsync());
                
                results.Add(await TestAnthropicCachingAsync());
                results.Add(await TestGeminiCachingAsync());
                results.Add(await TestOpenAICachingAsync());
                results.Add(await TestUnknownProviderCachingAsync());
                
                results.Add(await TestSmallContentNoCachingAsync());
                results.Add(await TestLargeContentCachingAsync());
                results.Add(await TestMultipleMessagesOrderingAsync());

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
                stopwatch.Stop();
                return CreateFailureResult(ex, stopwatch.Elapsed);
            }
        }

        private async Task<TestResult> TestCacheStrategyNoneAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.None);
                
                var request = CreateTestRequest("anthropic/claude-3.5-sonnet", "This is a very long message that would normally be cached but should not be cached when strategy is None. " + new string('x', 5000));
                
                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestCacheStrategyNone", "No messages found in request", TimeSpan.Zero);
                }
                
                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null);
                }
                
                return hasCaching 
                    ? TestResult.CreateFailure("TestCacheStrategyNone", "Cache strategy None should not apply any caching", TimeSpan.Zero)
                    : TestResult.CreateSuccess("TestCacheStrategyNone", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestCacheStrategyNone", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestCacheStrategyAutoAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                
                var request = CreateTestRequest("anthropic/claude-3.5-sonnet", "This is a very long message that should be cached with auto strategy. " + new string('x', 5000));
                
                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestCacheStrategyAuto", "No messages found in request", TimeSpan.Zero);
                }
                
                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null);
                }
                
                return hasCaching 
                    ? TestResult.CreateSuccess("TestCacheStrategyAuto", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestCacheStrategyAuto", "Cache strategy Auto should apply caching to large content with Anthropic model", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestCacheStrategyAuto", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestCacheStrategyAggressiveAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Aggressive);
                
                var request = CreateTestRequest("unknown/model", "This is a very long message that should be cached with aggressive strategy. " + new string('x', 5000));
                
                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestCacheStrategyAggressive", "No messages found in request", TimeSpan.Zero);
                }
                
                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null);
                }
                
                return hasCaching 
                    ? TestResult.CreateSuccess("TestCacheStrategyAggressive", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestCacheStrategyAggressive", "Cache strategy Aggressive should apply caching to large content even with unknown models", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestCacheStrategyAggressive", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestAnthropicCachingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                
                var request = new ChatRequest
                {
                    Model = "anthropic/claude-3.5-sonnet",
                    Messages = new Message[]
                    {
                        new Message { Role = "system", Content = "System message " + new string('a', 5000) },
                        new Message { Role = "user", Content = "User message 1 " + new string('b', 5000) },
                        new Message { Role = "assistant", Content = "Assistant message " + new string('c', 5000) },
                        new Message { Role = "user", Content = "User message 2 " + new string('d', 5000) },
                        new Message { Role = "user", Content = "User message 3 " + new string('e', 5000) }
                    }
                };
                
                ApplyCachingStrategyToRequest(client, request);
                
                var cachedCount = 0;
                if (request.Messages != null)
                {
                    foreach (var message in request.Messages)
                    {
                        if (message.Content is List<TextContentPart> parts && parts.Any(p => p.CacheControl != null))
                        {
                            cachedCount++;
                        }
                    }
                }
                
                return cachedCount <= 4 && cachedCount > 0
                    ? TestResult.CreateSuccess("TestAnthropicCaching", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestAnthropicCaching", $"Anthropic caching should apply to max 4 messages, but applied to {cachedCount}", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestAnthropicCaching", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestGeminiCachingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                

                var request = new ChatRequest
                {
                    Model = "google/gemini-2.5-pro",
                    Messages = new Message[]
                    {
                        new Message { Role = "system", Content = "System message " + new string('a', 3000) },
                        new Message { Role = "user", Content = "User message 1 " + new string('b', 8000) }, 
                        new Message { Role = "assistant", Content = "Assistant message " + new string('c', 5000) },
                        new Message { Role = "user", Content = "User message 2 " + new string('d', 4000) }
                    }
                };
                

                ApplyCachingStrategyToRequest(client, request);
                

                var cachedCount = 0;
                
                if (request.Messages != null)
                {
                    foreach (var message in request.Messages)
                    {
                        if (message.Content is List<TextContentPart> parts && parts.Any(p => p.CacheControl != null))
                        {
                            cachedCount++;
                        }
                    }
                }
                
                return cachedCount == 0
                    ? TestResult.CreateSuccess("TestGeminiCaching", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestGeminiCaching", $"Google/Gemini models should not have caching applied due to API incompatibility, but applied to {cachedCount} messages", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestGeminiCaching", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestOpenAICachingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                
                var request = CreateTestRequest("openai/gpt-4", "This is a very long message for OpenAI model. " + new string('x', 5000));
                

                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestOpenAICaching", "No messages found in request", TimeSpan.Zero);
                }

                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null);
                }
                else if (message.Content is string)
                {

                    hasCaching = false;
                }
                
                return !hasCaching 
                    ? TestResult.CreateSuccess("TestOpenAICaching", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestOpenAICaching", "OpenAI model should not have manual cache control applied", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestOpenAICaching", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestUnknownProviderCachingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                
                var request = CreateTestRequest("unknown/model", "This is a very long message for unknown model. " + new string('x', 5000));
                

                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestUnknownProviderCaching", "No messages found in request", TimeSpan.Zero);
                }

                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null);
                }
                
                return !hasCaching 
                    ? TestResult.CreateSuccess("TestUnknownProviderCaching", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestUnknownProviderCaching", "Unknown provider should not apply caching with Auto strategy", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestUnknownProviderCaching", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestSmallContentNoCachingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                
                var request = CreateTestRequest("anthropic/claude-3.5-sonnet", "This is a small message that should not be cached.");
                

                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestSmallContentNoCaching", "No messages found in request", TimeSpan.Zero);
                }

                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null);
                }
                
                return !hasCaching 
                    ? TestResult.CreateSuccess("TestSmallContentNoCaching", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestSmallContentNoCaching", "Small content should not be cached", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestSmallContentNoCaching", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestLargeContentCachingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                
                var request = CreateTestRequest("anthropic/claude-3.5-sonnet", "This is a large message that should be cached. " + new string('x', 5000));
                

                ApplyCachingStrategyToRequest(client, request);
                
                if (request.Messages == null || !request.Messages.Any())
                {
                    return TestResult.CreateFailure("TestLargeContentCaching", "No messages found in request", TimeSpan.Zero);
                }

                var message = request.Messages.First();
                bool hasCaching = false;
                
                if (message.Content is List<TextContentPart> parts)
                {
                    hasCaching = parts.Any(p => p.CacheControl != null && p.CacheControl.Type == "ephemeral");
                }
                
                return hasCaching 
                    ? TestResult.CreateSuccess("TestLargeContentCaching", TimeSpan.Zero)
                    : TestResult.CreateFailure("TestLargeContentCaching", "Large content should be cached with ephemeral cache control", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestLargeContentCaching", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private async Task<TestResult> TestMultipleMessagesOrderingAsync()
        {
            await Task.Delay(1); 
            
            try
            {
                var httpClient = new HttpClient();
                var client = new OpenRouterClient(httpClient, "test-key", CacheStrategy.Auto);
                

                var request = new ChatRequest
                {
                    Model = "anthropic/claude-3.5-sonnet",
                    Messages = new Message[]
                    {
                        new Message { Role = "user", Content = "Small message" }, 
                        new Message { Role = "user", Content = "Medium message " + new string('a', 6000) }, 
                        new Message { Role = "user", Content = "Large message " + new string('b', 8000) }, 
                        new Message { Role = "user", Content = "Another medium " + new string('c', 5000) } 
                    }
                };
                

                ApplyCachingStrategyToRequest(client, request);
                

                var cachedMessages = new List<(int index, int size, bool cached)>();
                
                if (request.Messages != null)
                {
                    for (int i = 0; i < request.Messages.Length; i++)
                    {
                        var message = request.Messages[i];
                        bool isCached = false;
                        int size = 0;
                        
                        if (message.Content is List<TextContentPart> parts)
                        {
                            isCached = parts.Any(p => p.CacheControl != null);
                            size = parts.Sum(p => p.Text?.Length ?? 0);
                        }
                        else if (message.Content is string stringContent)
                        {
                            size = stringContent.Length;
                            isCached = false; 
                        }
                        
                        cachedMessages.Add((i, size, isCached));
                    }
                }

                var cached = cachedMessages.Where(m => m.cached).ToList();
                var notCached = cachedMessages.Where(m => !m.cached).ToList();
                
                if (cached.Count != 3)
                {
                    return TestResult.CreateFailure("TestMultipleMessagesOrdering", 
                        $"Expected 3 cached messages, but found {cached.Count}", TimeSpan.Zero);
                }
                
                if (notCached.Count != 1 || notCached[0].index != 0)
                {
                    return TestResult.CreateFailure("TestMultipleMessagesOrdering", 
                        $"Expected small message (index 0) to not be cached", TimeSpan.Zero);
                }
                
                if (!cached.Any())
                {
                    return TestResult.CreateFailure("TestMultipleMessagesOrdering", 
                        "No cached messages found", TimeSpan.Zero);
                }

                var largestCached = cached.OrderByDescending(m => m.size).First();
                if (largestCached.index != 2)
                {
                    return TestResult.CreateFailure("TestMultipleMessagesOrdering", 
                        $"Expected largest message (index 2) to be cached, but largest cached was index {largestCached.index}", TimeSpan.Zero);
                }
                
                return TestResult.CreateSuccess("TestMultipleMessagesOrdering", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return TestResult.CreateFailure("TestMultipleMessagesOrdering", $"Exception: {ex.Message}", TimeSpan.Zero);
            }
        }

        private ChatRequest CreateTestRequest(string model, string content)
        {
            return new ChatRequest
            {
                Model = model,
                Messages = new Message[]
                {
                    new Message { Role = "user", Content = content }
                }
            };
        }

        private void ApplyCachingStrategyToRequest(OpenRouterClient client, ChatRequest request)
        {
            client.ApplyCachingStrategyToRequest(request);
        }
    }
} 
