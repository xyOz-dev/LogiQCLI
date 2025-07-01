using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter
{
    public class OpenRouterClient
    {
        private readonly HttpClient _httpClient;
        private readonly CacheStrategy _cacheStrategy;

        public OpenRouterClient(HttpClient httpClient, string? apiKey, CacheStrategy cacheStrategy = CacheStrategy.Auto)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/xyOz-dev");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "LogiQ");
            _cacheStrategy = cacheStrategy;
        }

        public async Task<ChatResponse> Chat(ChatRequest request, CancellationToken cancellationToken = default)
        {
            ApplyCachingStrategy(request);
            
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
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

        private void ApplyCachingStrategy(ChatRequest request)
        {
            if (request.Model == null || request.Messages == null || _cacheStrategy == CacheStrategy.None)
                return;

            var modelProvider = GetModelProvider(request.Model);
            var shouldApplyCache = ShouldApplyCache(modelProvider, request.Messages);

            if (!shouldApplyCache)
                return;

            switch (modelProvider)
            {
                case ModelProvider.Anthropic:
                    ApplyAnthropicCaching(request.Messages);
                    break;
                case ModelProvider.Google:
                    ApplyGeminiCaching(request.Messages);
                    break;
                case ModelProvider.OpenAI:
                case ModelProvider.Grok:
                case ModelProvider.DeepSeek:
                    break;
                default:
                    if (_cacheStrategy == CacheStrategy.Aggressive)
                    {
                        ApplyGenericCaching(request.Messages);
                    }
                    break;
            }
        }

        private ModelProvider GetModelProvider(string model)
        {
            var modelLower = model.ToLowerInvariant();
            
            if (modelLower.Contains("anthropic") || modelLower.Contains("claude"))
                return ModelProvider.Anthropic;
            if (modelLower.Contains("google") || modelLower.Contains("gemini"))
                return ModelProvider.Google;
            if (modelLower.Contains("openai") || modelLower.Contains("gpt"))
                return ModelProvider.OpenAI;
            if (modelLower.Contains("grok"))
                return ModelProvider.Grok;
            if (modelLower.Contains("deepseek"))
                return ModelProvider.DeepSeek;
            
            return ModelProvider.Unknown;
        }

        private bool ShouldApplyCache(ModelProvider provider, Message[] messages)
        {
            if (_cacheStrategy == CacheStrategy.None)
                return false;

            if (provider == ModelProvider.OpenAI || provider == ModelProvider.Grok || provider == ModelProvider.DeepSeek)
                return false;

            return messages.Any(msg => HasCacheableContent(msg));
        }

        private bool HasCacheableContent(Message message)
        {
            const int minCacheableLength = 4000;

            if (message.Content is string stringContent)
            {
                return stringContent.Length >= minCacheableLength;
            }

            if (message.Content is List<TextContentPart> contentParts)
            {
                var totalLength = contentParts.Where(p => p.Text != null).Sum(p => p.Text.Length);
                return totalLength >= minCacheableLength;
            }

            return false;
        }

        private void ApplyAnthropicCaching(Message[] messages)
        {
            var cacheableMessages = messages
                .Where(HasCacheableContent)
                .OrderByDescending(msg => GetContentLength(msg))
                .Take(4)
                .ToArray();

            foreach (var message in cacheableMessages)
            {
                ApplyCacheControlToMessage(message, "ephemeral");
            }
        }

        private void ApplyGeminiCaching(Message[] messages)
        {
            var largestMessage = messages
                .Where(HasCacheableContent)
                .OrderByDescending(GetContentLength)
                .FirstOrDefault();

            if (largestMessage != null)
            {
                ApplyCacheControlToMessage(largestMessage, "ephemeral");
            }
        }

        private void ApplyGenericCaching(Message[] messages)
        {
            var largestMessage = messages
                .Where(HasCacheableContent)
                .OrderByDescending(GetContentLength)
                .FirstOrDefault();

            if (largestMessage != null)
            {
                ApplyCacheControlToMessage(largestMessage, "ephemeral");
            }
        }

        private int GetContentLength(Message message)
        {
            if (message.Content is string stringContent)
                return stringContent.Length;

            if (message.Content is List<TextContentPart> contentParts)
                return contentParts.Where(p => p.Text != null).Sum(p => p.Text.Length);

            return 0;
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
                var largestPart = contentParts
                    .Where(p => p.Text != null)
                    .OrderByDescending(p => p.Text.Length)
                    .FirstOrDefault();

                if (largestPart != null)
                {
                    largestPart.CacheControl = new CacheControl { Type = cacheType };
                }
            }
        }
    }

    public enum CacheStrategy
    {
        None,
        Auto,
        Aggressive
    }

    public enum ModelProvider
    {
        Unknown,
        Anthropic,
        Google,
        OpenAI,
        Grok,
        DeepSeek
    }
}
