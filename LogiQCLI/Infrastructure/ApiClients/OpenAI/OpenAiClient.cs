using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenAI
{
    public sealed class OpenAiClient
    {
        private readonly HttpClient _httpClient;

        public OpenAiClient(HttpClient httpClient, string apiKey, string? baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            if (_httpClient.BaseAddress == null)
            {
                var finalBase = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1" : baseUrl.TrimEnd('/');
                _httpClient.BaseAddress = new Uri(finalBase, UriKind.Absolute);
            }
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var jsonPayload = JsonSerializer.Serialize(request, options);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            return result ?? new ChatResponse();
        }

        public async Task<List<Objects.OpenAiModel>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("/models", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<Objects.OpenAiModel>();
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var list = JsonSerializer.Deserialize<Objects.OpenAiModelListResponse>(responseBody);
            return list?.Data ?? new List<Objects.OpenAiModel>();
        }

        public async Task<Objects.EmbeddingResponse> CreateEmbeddingAsync(Objects.EmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var jsonPayload = JsonSerializer.Serialize(request, options);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/embeddings", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<Objects.EmbeddingResponse>(responseBody);
            return result ?? new Objects.EmbeddingResponse();
        }
    }
} 