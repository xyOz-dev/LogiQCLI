using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.Providers;

namespace LogiQCLI.Infrastructure.ApiClients.Requesty
{
    public sealed class RequestyClient
    {
        private readonly HttpClient _http;

        public RequestyClient(HttpClient http, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));

            _http = http;
            _http.BaseAddress = new Uri("https://router.requesty.ai/v1/");
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            
            _http.DefaultRequestHeaders.Remove("HTTP-Referer");
            _http.DefaultRequestHeaders.Remove("X-Title");
            
            _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/xyOz-dev/LogiQCLI");
            _http.DefaultRequestHeaders.Add("X-Title", "LogiQCLI");
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var payload = new StringContent(JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("chat/completions", payload, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Requesty {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }
            var json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ChatResponse>(json) ?? new ChatResponse();
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken ct = default)
        {
            var resp = await _http.GetAsync("models", ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            var models = JsonSerializer.Deserialize<ModelListResponse>(json);
            return models?.Data ?? new List<Model>();
        }
    }
} 