using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter
{
    public class OpenRouterClient
    {
        private readonly HttpClient _httpClient;
        private readonly ProviderPreferencesManager _providerPreferencesManager;
        private readonly CacheManager _cacheManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public OpenRouterClient(HttpClient httpClient, string? apiKey, CacheStrategy cacheStrategy = CacheStrategy.Auto)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
            _httpClient.DefaultRequestHeaders.Remove("X-Title");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/xyOz-dev/LogiQCLI");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "LogiQCLI");
            
            _providerPreferencesManager = new ProviderPreferencesManager(httpClient);
            _cacheManager = new CacheManager(cacheStrategy);
            _jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        }

        public async Task<ChatResponse> Chat(ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Provider == null)
            {
                request.Provider = await _providerPreferencesManager.BuildProviderPreferencesAsync(request);
            }

            _cacheManager.ApplyCachingStrategy(request);
            
            var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            
            _cacheManager.HandleCacheResponse(result, _providerPreferencesManager);
            
            return result ?? new ChatResponse();
        }

        public async Task<List<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("https://openrouter.ai/api/v1/models", cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ModelListResponse>(responseBody);
            return result?.Data ?? new List<Model>();
        }

        public async Task<Objects.ModelEndpointsData> GetModelEndpointsAsync(string author, string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(author)) throw new ArgumentNullException(nameof(author));
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentNullException(nameof(slug));

            var url = $"https://openrouter.ai/api/v1/models/{author}/{slug}/endpoints";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get model endpoints: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<Objects.ModelEndpointsResponse>(json);
            return data?.Data ?? new Objects.ModelEndpointsData();
        }

        public void ApplyCachingStrategyToRequest(ChatRequest request)
        {
            _cacheManager.ApplyCachingStrategy(request);
        }
    }

    public class ProviderPreferencesManager
    {
        private readonly Dictionary<string, ProviderCapabilities> _providerCapabilities;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, (Objects.ModelEndpointsData Data, DateTime Expires)> _endpointCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _endpointTtl = TimeSpan.FromMinutes(10);
        private string? _currentCacheProvider;

        public ProviderPreferencesManager(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _providerCapabilities = InitializeProviderCapabilities();
        }

        public async Task<Provider> BuildProviderPreferencesAsync(ChatRequest request)
        {
            if (request.Provider != null)
            {
                return request.Provider;
            }

            var preferences = new Provider
            {
                RequireParameters = request.Tools?.Any() == true,
                DataCollection = "allow",
                AllowFallbacks = true,
                Sort = "price" // default cheapest
            };

            if (!string.IsNullOrWhiteSpace(request.Model) && request.Model!.Contains('/'))
            {
                var parts = request.Model.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var author = parts[0];
                    var slugWithVariant = parts[1];

                    var slug = slugWithVariant.Split(':')[0];

                    var endpointsData = await GetEndpointsDataAsync(author, slug);
                    if (endpointsData?.Endpoints?.Count > 0)
                    {
                        var endpoints = endpointsData.Endpoints;

                        if (request.Tools?.Any() == true)
                        {
                            endpoints = endpoints.Where(e => e.SupportedParameters.Any(p => string.Equals(p, "tools", StringComparison.OrdinalIgnoreCase))).ToList();
                        }

                        if (endpoints.Count > 0)
                        {
                            var providerTags = endpoints.Select(e => e.Tag ?? e.ProviderName.ToLowerInvariant()).Distinct().ToArray();
                            preferences.Only = providerTags;
                        }
                    }
                }
            }

            if (preferences.Only == null || preferences.Only.Length == 0)
            {
                if (request.Tools?.Any() == true)
                {
                    var toolSupportingProviders = GetToolSupportingProviders();
                    preferences.Order = toolSupportingProviders.ToArray();

                    if (_currentCacheProvider != null && toolSupportingProviders.Contains(_currentCacheProvider))
                    {
                        preferences.Only = new[] { _currentCacheProvider };
                        preferences.AllowFallbacks = false;
                    }
                }
                else if (_currentCacheProvider != null)
                {
                    preferences.Order = new[] { _currentCacheProvider };
                    preferences.AllowFallbacks = false;
                }
            }

            return preferences;
        }

        private async Task<Objects.ModelEndpointsData?> GetEndpointsDataAsync(string author, string slug)
        {
            var key = $"{author}/{slug}".ToLowerInvariant();
            if (_endpointCache.TryGetValue(key, out var cached) && cached.Expires > DateTime.UtcNow)
            {
                return cached.Data;
            }

            try
            {
                var url = $"https://openrouter.ai/api/v1/models/{author}/{slug}/endpoints";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<Objects.ModelEndpointsResponse>(json);
                if (data?.Data != null)
                {
                    _endpointCache[key] = (data.Data, DateTime.UtcNow.Add(_endpointTtl));
                }
                return data?.Data;
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<string> GetToolSupportingProviders()
        {
            return _providerCapabilities
                .Where(kvp => kvp.Value.SupportsTools)
                .Select(kvp => kvp.Key);
        }

        private Dictionary<string, ProviderCapabilities> InitializeProviderCapabilities()
        {
            return new Dictionary<string, ProviderCapabilities>
            {
                ["anthropic"] = new ProviderCapabilities(true, true, true),
                ["openai"] = new ProviderCapabilities(true, false, true),
                ["google"] = new ProviderCapabilities(true, false, true),
                ["meta"] = new ProviderCapabilities(true, false, false),
                ["mistral"] = new ProviderCapabilities(true, false, false),
                ["cohere"] = new ProviderCapabilities(true, false, false),
                ["x-ai"] = new ProviderCapabilities(true, false, false),
                ["deepseek"] = new ProviderCapabilities(true, true, false),
                ["together"] = new ProviderCapabilities(true, false, false),
                ["fireworks"] = new ProviderCapabilities(true, false, false),
                ["huggingface"] = new ProviderCapabilities(false, false, false),
                ["replicate"] = new ProviderCapabilities(false, false, false),
                ["perplexity"] = new ProviderCapabilities(true, false, false),
                ["nvidia"] = new ProviderCapabilities(true, false, false),
                ["lepton"] = new ProviderCapabilities(true, false, false),
                ["hyperbolic"] = new ProviderCapabilities(true, false, false),
                ["cerebras"] = new ProviderCapabilities(false, false, false),
                ["lambda"] = new ProviderCapabilities(false, false, false),
                ["groq"] = new ProviderCapabilities(true, false, false),
                ["deepinfra"] = new ProviderCapabilities(true, false, false),
                ["openrouter"] = new ProviderCapabilities(false, false, false),
                ["stealth"] = new ProviderCapabilities(false, false, false)
            };
        }

        public void SetCacheProvider(string provider)
        {
            _currentCacheProvider = provider;
        }
    }

    public class CacheManager
    {
        private readonly CacheStrategy _cacheStrategy;
        private readonly Dictionary<string, ProviderCacheInfo> _providerCacheSupport;

        public CacheManager(CacheStrategy cacheStrategy)
        {
            _cacheStrategy = cacheStrategy;
            _providerCacheSupport = new Dictionary<string, ProviderCacheInfo>
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

            var cacheInfo = _providerCacheSupport.GetValueOrDefault(targetProvider, new ProviderCacheInfo(false, CacheType.None, 0, 0));
            
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
                    lastTool.CacheControl = LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects.CacheControl.Ephemeral();
                    breakpointsRemaining -= 1;
                }
            }

            if (breakpointsRemaining <= 0)
                return;

            var cacheableMessages = GetCacheableMessages(request.Messages ?? Array.Empty<Message>(), cacheInfo, breakpointsRemaining);
            if (!cacheableMessages.Any())
                return;

            ApplyCacheToMessages(cacheableMessages, targetProvider, cacheInfo);
        }

        public void HandleCacheResponse(ChatResponse? response, ProviderPreferencesManager providerManager)
        {
            if (response != null)
            {
            }
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

        private string? InferProviderFromModel(string? model)
        {
            if (string.IsNullOrEmpty(model))
                return null;

            var modelLower = model.ToLowerInvariant();
            
            if (modelLower.Contains("anthropic") || modelLower.Contains("claude"))
                return "anthropic";
            if (modelLower.Contains("google") || modelLower.Contains("gemini"))
                return "google";
            if (modelLower.Contains("openai") || modelLower.Contains("gpt"))
                return "openai";
            if (modelLower.Contains("grok"))
                return "x-ai";
            if (modelLower.Contains("deepseek"))
                return "deepseek";
            if (modelLower.Contains("meta") || modelLower.Contains("llama"))
                return "meta";
            if (modelLower.Contains("mistral"))
                return "mistral";
            if (modelLower.Contains("cohere"))
                return "cohere";
            if (modelLower.Contains("openrouter"))
                return "openrouter";

            if (modelLower.Contains("stealth"))
                return "stealth";
            
            return null;
        }

        private IEnumerable<Message> GetCacheableMessages(Message[] messages, ProviderCacheInfo cacheInfo, int maxToTake)
        {
            const int estimatedCharsPerToken = 4;
            int minCacheableLength;
            
            if (cacheInfo.MinTokens == 0)
            {
                minCacheableLength = 4000;
            }
            else
            {
                minCacheableLength = cacheInfo.MinTokens * estimatedCharsPerToken;
            }

            return messages
                .Where(m => GetContentLength(m) >= minCacheableLength)
                .OrderByDescending(GetContentLength)
                .Take(maxToTake);
        }

        private int GetContentLength(Message message)
        {
            if (message.Content is string stringContent)
                return stringContent.Length;

            if (message.Content is List<TextContentPart> contentParts)
                return contentParts.Where(p => p.Text != null).Sum(p => p.Text.Length);

            return 0;
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

        private string GetCacheType(string provider)
        {
            return provider switch
            {
                "anthropic" => "ephemeral",
                "google" => "ephemeral",
                "deepseek" => "ephemeral",
                _ => "ephemeral"
            };
        }

        private void ApplyCacheControlToMessage(Message message, string cacheType)
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

    public record ProviderCapabilities(bool SupportsTools, bool SupportsCache, bool SupportsStreaming);

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
