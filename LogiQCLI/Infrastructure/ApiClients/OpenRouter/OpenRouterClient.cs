using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Services;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter
{
    public class OpenRouterClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IProviderPreferencesManager _providerPreferencesManager;
        private readonly ICacheService _cacheService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly bool _disposeHttpClient;

        public OpenRouterClient(HttpClient httpClient, string? apiKey, CacheStrategy cacheStrategy = CacheStrategy.Auto)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new OpenRouterConfigurationException("API key cannot be null or empty");

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = false;

            ConfigureHttpClient(apiKey);
            
            _providerPreferencesManager = new ProviderPreferencesService(_httpClient);
            _cacheService = new CacheService(cacheStrategy);
            _jsonOptions = new JsonSerializerOptions 
            { 
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
            };
        }

        public OpenRouterClient(string apiKey, CacheStrategy cacheStrategy = CacheStrategy.Auto)
            : this(new HttpClient(), apiKey, cacheStrategy)
        {
            _disposeHttpClient = true;
        }

        public async Task<ChatResponse> Chat(ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                await PrepareRequestAsync(request).ConfigureAwait(false);

                var content = SerializeRequest(request);
                var response = await SendRequestAsync(content, cancellationToken).ConfigureAwait(false);
                var result = await ProcessResponseAsync(response).ConfigureAwait(false);
                
                _cacheService.HandleCacheResponse(result, _providerPreferencesManager);
                
                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new OpenRouterApiException(0, string.Empty, $"Network error: {ex.Message}");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new OpenRouterException("TIMEOUT", "Request timed out");
            }
            catch (JsonException ex)
            {
                throw new OpenRouterException("SERIALIZATION_ERROR", $"Failed to process response: {ex.Message}");
            }
        }

        public async Task<List<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("https://openrouter.ai/api/v1/models", cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new OpenRouterApiException((int)response.StatusCode, errorBody, 
                        $"Failed to get models: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<ModelListResponse>(responseBody);
                return result?.Data ?? new List<Model>();
            }
            catch (HttpRequestException ex)
            {
                throw new OpenRouterApiException(0, string.Empty, $"Network error while fetching models: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenRouterException("SERIALIZATION_ERROR", $"Failed to deserialize models response: {ex.Message}");
            }
        }

        public async Task<ModelEndpointsData> GetModelEndpointsAsync(string author, string slug, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(author)) 
                throw new ArgumentException("Author cannot be null or empty", nameof(author));
            if (string.IsNullOrWhiteSpace(slug)) 
                throw new ArgumentException("Slug cannot be null or empty", nameof(slug));

            try
            {
                var url = $"https://openrouter.ai/api/v1/models/{author}/{slug}/endpoints";
                var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new OpenRouterApiException((int)response.StatusCode, body,
                        $"Failed to get model endpoints for {author}/{slug}: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<ModelEndpointsResponse>(json);
                return data?.Data ?? new ModelEndpointsData();
            }
            catch (HttpRequestException ex)
            {
                throw new OpenRouterApiException(0, string.Empty, 
                    $"Network error while fetching endpoints for {author}/{slug}: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenRouterException("SERIALIZATION_ERROR", 
                    $"Failed to deserialize endpoints response for {author}/{slug}: {ex.Message}");
            }
        }

        public void ApplyCachingStrategyToRequest(ChatRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
                
            _cacheService.ApplyCachingStrategy(request);
        }

        private void ConfigureHttpClient(string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
            _httpClient.DefaultRequestHeaders.Remove("X-Title");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/xyOz-dev/LogiQCLI");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "LogiQCLI");
        }

        private async Task PrepareRequestAsync(ChatRequest request)
        {
            if (request.Provider == null)
            {
                request.Provider = await _providerPreferencesManager.BuildProviderPreferencesAsync(request)
                    .ConfigureAwait(false);
            }

            _cacheService.ApplyCachingStrategy(request);
        }

        private StringContent SerializeRequest(ChatRequest request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<HttpResponseMessage> SendRequestAsync(StringContent content, CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", 
                content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response).ConfigureAwait(false);
            }

            return response;
        }

        private async Task HandleErrorResponseAsync(HttpResponseMessage response)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;

            switch (statusCode)
            {
                case 429:
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    throw new OpenRouterRateLimitException(
                        $"Rate limit exceeded. Response: {errorBody}", retryAfter);
                case 401:
                    throw new OpenRouterConfigurationException($"Authentication failed: {errorBody}");
                case 403:
                    throw new OpenRouterConfigurationException($"Access forbidden: {errorBody}");
                default:
                    throw new OpenRouterApiException(statusCode, errorBody,
                        $"API request failed with status {statusCode}: {response.ReasonPhrase}");
            }
        }

        private async Task<ChatResponse> ProcessResponseAsync(HttpResponseMessage response)
        {
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            return result ?? new ChatResponse();
        }

        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
