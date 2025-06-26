using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.Tavily.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.Tavily
{
    public class TavilyClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public TavilyClient(HttpClient httpClient, string apiKey, string? baseUrl = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _baseUrl = baseUrl ?? "https://api.tavily.com";
        }

        public async Task<TavilySearchResponse> SearchAsync(TavilySearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.Query))
                throw new ArgumentException("Query cannot be null or empty", nameof(request));

            request.ApiKey = _apiKey;

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/search", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new TavilyException($"Tavily API request failed with status {response.StatusCode}: {responseContent}");
                }

                var searchResponse = JsonSerializer.Deserialize<TavilySearchResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                return searchResponse ?? throw new TavilyException("Failed to deserialize Tavily API response");
            }
            catch (HttpRequestException ex)
            {
                throw new TavilyException($"Network error calling Tavily API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TavilyException($"Tavily API request timed out: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new TavilyException($"Failed to parse Tavily API response: {ex.Message}", ex);
            }
        }
    }
} 