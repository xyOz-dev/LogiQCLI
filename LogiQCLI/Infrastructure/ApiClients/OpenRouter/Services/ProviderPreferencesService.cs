using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Services
{
    public interface IProviderPreferencesManager
    {
        Task<Provider> BuildProviderPreferencesAsync(ChatRequest request);
        void SetCacheProvider(string provider);
    }

    public class ProviderPreferencesService : IProviderPreferencesManager
    {
        private readonly Dictionary<string, ProviderCapabilities> _providerCapabilities;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, (ModelEndpointsData Data, DateTime Expires)> _endpointCache;
        private readonly TimeSpan _endpointTtl;
        private string? _currentCacheProvider;

        public ProviderPreferencesService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _providerCapabilities = InitializeProviderCapabilities();
            _endpointCache = new Dictionary<string, (ModelEndpointsData Data, DateTime Expires)>(StringComparer.OrdinalIgnoreCase);
            _endpointTtl = TimeSpan.FromMinutes(10);
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
                Sort = "price"
            };

            if (!string.IsNullOrWhiteSpace(request.Model) && request.Model!.Contains('/'))
            {
                var parts = request.Model.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var author = parts[0];
                    var slugWithVariant = parts[1];
                    var slug = slugWithVariant.Split(':')[0];

                    var endpointsData = await GetEndpointsDataAsync(author, slug).ConfigureAwait(false);
                    if (endpointsData?.Endpoints?.Count > 0)
                    {
                        var endpoints = endpointsData.Endpoints;

                        if (request.Tools?.Any() == true)
                        {
                            endpoints = endpoints.Where(e => e.SupportedParameters.Any(p => 
                                string.Equals(p, "tools", StringComparison.OrdinalIgnoreCase))).ToList();
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

        public void SetCacheProvider(string provider)
        {
            _currentCacheProvider = provider;
        }

        private async Task<ModelEndpointsData?> GetEndpointsDataAsync(string author, string slug)
        {
            var key = $"{author}/{slug}".ToLowerInvariant();
            if (_endpointCache.TryGetValue(key, out var cached) && cached.Expires > DateTime.UtcNow)
            {
                return cached.Data;
            }

            try
            {
                var url = $"https://openrouter.ai/api/v1/models/{author}/{slug}/endpoints";
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<ModelEndpointsResponse>(json);
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

        private static Dictionary<string, ProviderCapabilities> InitializeProviderCapabilities()
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
    }

    public record ProviderCapabilities(bool SupportsTools, bool SupportsCache, bool SupportsStreaming);
} 