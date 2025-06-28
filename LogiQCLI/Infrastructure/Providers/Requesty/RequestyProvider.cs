using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.Providers.Objects;

namespace LogiQCLI.Infrastructure.Providers.Requesty
{
    /// <summary>
    /// Simple HTTP-based implementation of ILlmProvider that calls the Requesty router.
    /// </summary>
    public sealed class RequestyProvider : ILlmProvider, IDisposable
    {
        private readonly HttpClient _httpClient;

        public RequestyProvider(HttpClient httpClient, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://router.requesty.ai/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/xyOz-dev/LogiQCLI");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "LogiQCLI");
        }

        public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var content = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Requesty error {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {body}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(json);
            return result ?? new ChatCompletionResponse();
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("models", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var list = JsonSerializer.Deserialize<ModelListResponse>(json);
            return list?.Data ?? new List<Model>();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 