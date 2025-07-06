using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Services
{
    public interface ICacheService
    {
        void ApplyCachingStrategy(ChatRequest request);
        void HandleCacheResponse(ChatResponse? response, IProviderPreferencesManager providerManager);
    }

    public class CacheService : ICacheService
    {
        private readonly CacheStrategy _cacheStrategy;
        private readonly Dictionary<string, ProviderCacheInfo> _providerCacheSupport;

        public CacheService(CacheStrategy cacheStrategy)
        {
            _cacheStrategy = cacheStrategy;
            _providerCacheSupport = InitializeProviderCacheSupport();
        }

        public void ApplyCachingStrategy(ChatRequest request)
        {
            if (_cacheStrategy == CacheStrategy.None || (request.Messages == null && request.Tools == null))
                return;

            var targetProvider = GetTargetProvider(request);
            if (targetProvider == null)
            {
                targetProvider = InferProviderFromModel(request.Model);
            }

            if (targetProvider == null)
            {
                if (_cacheStrategy == CacheStrategy.Aggressive)
                {
                    targetProvider = "unknown";
                }
                else
                {
                    return;
                }
            }

            var cacheInfo = _providerCacheSupport.GetValueOrDefault(targetProvider, 
                new ProviderCacheInfo(false, CacheType.None, 0, 0));
            
            if (!cacheInfo.SupportsCache && _cacheStrategy != CacheStrategy.Aggressive)
                return;

            if (targetProvider == "unknown" && _cacheStrategy == CacheStrategy.Aggressive)
            {
                cacheInfo = new ProviderCacheInfo(true, CacheType.Explicit, 0, 4);
            }

            var breakpointsRemaining = cacheInfo.MaxBreakpoints > 0 ? cacheInfo.MaxBreakpoints : 4;
            if (targetProvider == "anthropic" && request.Tools?.Any() == true)
            {
                var lastTool = request.Tools.Last();
                if (lastTool.CacheControl == null)
                {
                    lastTool.CacheControl = CacheControl.Ephemeral();
                    breakpointsRemaining -= 1;
                }
            }

            if (breakpointsRemaining <= 0)
                return;

            var cacheableMessages = GetCacheableMessages(request.Messages ?? Array.Empty<Message>(), 
                cacheInfo, breakpointsRemaining);
            if (!cacheableMessages.Any())
                return;

            ApplyCacheToMessages(cacheableMessages, targetProvider, cacheInfo);
        }

        public void HandleCacheResponse(ChatResponse? response, IProviderPreferencesManager providerManager)
        {
        }

        private Dictionary<string, ProviderCacheInfo> InitializeProviderCacheSupport()
        {
            return new Dictionary<string, ProviderCacheInfo>
            {
                ["anthropic"] = new ProviderCacheInfo(true, CacheType.Explicit, 1000, 4),
                ["openai"] = new ProviderCacheInfo(true, CacheType.Automated, 1024, 0),
                ["x-ai"] = new ProviderCacheInfo(true, CacheType.Automated, 1000, 0),
                ["deepseek"] = new ProviderCacheInfo(true, CacheType.Automated, 1000, 0),
                ["google"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["meta"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["mistral"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["cohere"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["together"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["fireworks"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["huggingface"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["replicate"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["perplexity"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["nvidia"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["lepton"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["hyperbolic"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["cerebras"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["lambda"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["groq"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["deepinfra"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["openrouter"] = new ProviderCacheInfo(false, CacheType.None, 0, 0),
                ["stealth"] = new ProviderCacheInfo(false, CacheType.None, 0, 0)
            };
        }

        private string? GetTargetProvider(ChatRequest request)
        {
            var inferred = InferProviderFromModel(request.Model);
            if (!string.IsNullOrEmpty(inferred))
            {
                return inferred;
            }

            if (request.Provider?.Order?.Any() == true)
                return request.Provider.Order.First();

            return null;
        }

        private static string? InferProviderFromModel(string? model)
        {
            if (string.IsNullOrEmpty(model))
                return null;

            var modelLower = model.ToLowerInvariant();
            
            return modelLower switch
            {
                _ when modelLower.Contains("anthropic") || modelLower.Contains("claude") => "anthropic",
                _ when modelLower.Contains("google") || modelLower.Contains("gemini") => "google",
                _ when modelLower.Contains("openai") || modelLower.Contains("gpt") => "openai",
                _ when modelLower.Contains("grok") => "x-ai",
                _ when modelLower.Contains("deepseek") => "deepseek",
                _ when modelLower.Contains("meta") || modelLower.Contains("llama") => "meta",
                _ when modelLower.Contains("mistral") => "mistral",
                _ when modelLower.Contains("cohere") => "cohere",
                _ when modelLower.Contains("openrouter") => "openrouter",
                _ when modelLower.Contains("stealth") => "stealth",
                _ => null
            };
        }

        private IEnumerable<Message> GetCacheableMessages(Message[] messages, ProviderCacheInfo cacheInfo, int maxToTake)
        {
            const int estimatedCharsPerToken = 4;
            int minCacheableLength = cacheInfo.MinTokens == 0 ? 4000 : cacheInfo.MinTokens * estimatedCharsPerToken;

            return messages
                .Where(m => GetContentLength(m) >= minCacheableLength)
                .OrderByDescending(GetContentLength)
                .Take(maxToTake);
        }

        private static int GetContentLength(Message message)
        {
            return message.Content switch
            {
                string stringContent => stringContent.Length,
                List<TextContentPart> contentParts => contentParts.Where(p => p.Text != null).Sum(p => p.Text.Length),
                _ => 0
            };
        }

        private void ApplyCacheToMessages(IEnumerable<Message> messages, string provider, ProviderCacheInfo cacheInfo)
        {
            if (cacheInfo.CacheType == CacheType.Automated)
            {
                return;
            }

            var cacheType = GetCacheType(provider);
            var messageList = messages.ToList();
            
            if (cacheInfo.CacheType == CacheType.Both && provider == "google")
            {
                var lastMessage = messageList.LastOrDefault();
                if (lastMessage != null)
                {
                    ApplyCacheControlToMessage(lastMessage, cacheType);
                }
            }
            else if (cacheInfo.CacheType == CacheType.Explicit)
            {
                foreach (var message in messageList)
                {
                    ApplyCacheControlToMessage(message, cacheType);
                }
            }
        }

        private static string GetCacheType(string provider)
        {
            return provider switch
            {
                "anthropic" => "ephemeral",
                "google" => "ephemeral",
                "deepseek" => "ephemeral",
                _ => "ephemeral"
            };
        }

        private static void ApplyCacheControlToMessage(Message message, string cacheType)
        {
            if (message.Content is string stringContent && !string.IsNullOrEmpty(stringContent))
            {
                message.Content = new List<TextContentPart>
                {
                    new TextContentPart
                    {
                        Text = stringContent,
                        CacheControl = new CacheControl { Type = cacheType }
                    }
                };
            }
            else if (message.Content is List<TextContentPart> contentParts)
            {
                var lastPart = contentParts.LastOrDefault();
                if (lastPart?.Text != null)
                {
                    lastPart.CacheControl = new CacheControl { Type = cacheType };
                }
            }
        }
    }

    public record ProviderCacheInfo(bool SupportsCache, CacheType CacheType, int MinTokens, int MaxBreakpoints);

    public enum CacheType
    {
        None,
        Automated,
        Explicit,
        Both
    }

    public enum CacheStrategy
    {
        None,
        Auto,
        Aggressive
    }
} 