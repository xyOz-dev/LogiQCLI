using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.LMStudio
{
    public sealed class LMStudioClient
    {
        private readonly HttpClient _httpClient;

        public LMStudioClient(HttpClient httpClient, string? baseUrl = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            if (_httpClient.BaseAddress == null)
            {
                var finalBase = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:1234" : baseUrl.TrimEnd('/');
                _httpClient.BaseAddress = new Uri(finalBase, UriKind.Absolute);
            }
        }



        public async Task<LMStudioChatResponse> Chat(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var jsonPayload = JsonSerializer.Serialize(request, options);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v0/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<LMStudioChatResponse>(responseBody);
            return result ?? new LMStudioChatResponse();
        }

        public async Task<List<LMStudioModel>> GetModelsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("/api/v0/models", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<LMStudioModel>();
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<LMStudioModelListResponse>(responseBody);
            return result?.Data ?? new List<LMStudioModel>();
        }
    }
} 