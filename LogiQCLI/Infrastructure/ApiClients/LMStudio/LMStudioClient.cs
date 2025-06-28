using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.LMStudio
{
    /// <summary>
    /// Lightweight API client for interacting with an LM Studio local server.
    /// LM Studio exposes an OpenAI-compatible HTTP API when the built-in server is enabled
    /// (e.g. http://localhost:1234/v1/chat/completions).
    /// This client purposefully mirrors the public surface area of <see cref="OpenRouterClient"/>
    /// so that it can be used interchangeably by the existing infrastructure.
    /// </summary>
    public sealed class LMStudioClient
    {
        private readonly HttpClient _httpClient;

        /// <param name="httpClient">The HttpClient instance to use. Caller may provide a pre-configured instance with BaseAddress set.</param>
        /// <param name="baseUrl">Optional base URL of the LM Studio server. Defaults to "http://localhost:1234" if not provided and <paramref name="httpClient"/> has no BaseAddress.</param>
        public LMStudioClient(HttpClient httpClient, string? baseUrl = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Ensure the HttpClient has a base address.
            if (_httpClient.BaseAddress == null)
            {
                var finalBase = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:1234" : baseUrl.TrimEnd('/');
                _httpClient.BaseAddress = new Uri(finalBase, UriKind.Absolute);
            }
        }

        /// <summary>
        /// Sends a chat completion request to LM Studio.
        /// </summary>
        public async Task<ChatResponse> Chat(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var jsonPayload = JsonSerializer.Serialize(request, options);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Endpoint path is identical to the OpenAI compatible specification.
            var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            return result ?? new ChatResponse();
        }

        /// <summary>
        /// Attempts to retrieve the list of available models from LM Studio. Returns an empty list
        /// if the endpoint is not implemented (older versions of LM Studio). The endpoint follows the
        /// OpenAI convention: GET /v1/models.
        /// </summary>
        public async Task<List<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("/v1/models", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Some LM Studio versions do not expose /v1/models – fall back to empty list.
                return new List<Model>();
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ModelListResponse>(responseBody);
            return result?.Data ?? new List<Model>();
        }
    }
} 