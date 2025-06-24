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

        public OpenRouterClient(HttpClient httpClient, string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/xyOz-dev/LogiQCLI");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "LogiQCLI");
        }

        public async Task<ChatResponse> Chat(ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Model != null && (request.Model.StartsWith("anthropic/") || request.Model.StartsWith("google/")) && request.Messages != null)
            {
                foreach (var message in request.Messages)
                {
                    if ((message.Role == "user" || message.Role == "tool") && message.Content is string stringContent && !string.IsNullOrEmpty(stringContent))
                    {
                        message.Content = new List<TextContentPart>
                        {
                            new TextContentPart
                            {
                                Text = stringContent,
                                CacheControl = new CacheControl()
                            }
                        };
                    }
                }
            }
            
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
    }
}
