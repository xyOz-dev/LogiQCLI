using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.Documentation.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.Documentation
{
    public class DocumentationApiClient
    {
        private const string API_BASE_URL = "https://context7.com/api";
        private const string DEFAULT_TYPE = "txt";
        private readonly HttpClient _httpClient;

        public DocumentationApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SearchResponse> SearchLibrariesAsync(string query)
        {
            try
            {
                var url = $"{API_BASE_URL}/v1/search?query={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = (int)response.StatusCode;
                    if (errorCode == 429)
                    {
                        return new SearchResponse
                        {
                            Results = new List<SearchResult>(),
                            Error = "Rate limited due to too many requests. Please try again later."
                        };
                    }
                    return new SearchResponse
                    {
                        Results = new List<SearchResult>(),
                        Error = $"Failed to search libraries. Please try again later. Error code: {errorCode}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<SearchResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return searchResponse ?? new SearchResponse { Results = new List<SearchResult>() };
            }
            catch (Exception ex)
            {
                return new SearchResponse
                {
                    Results = new List<SearchResult>(),
                    Error = $"Error searching libraries: {ex.Message}"
                };
            }
        }

        public async Task<string?> FetchDocumentationAsync(string libraryId, int tokens, string? topic = null)
        {
            try
            {
                if (libraryId.StartsWith("/"))
                {
                    libraryId = libraryId.Substring(1);
                }

                var url = $"{API_BASE_URL}/v1/{libraryId}?tokens={tokens}&type={DEFAULT_TYPE}";
                if (!string.IsNullOrEmpty(topic))
                {
                    url += $"&topic={Uri.EscapeDataString(topic)}";
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Context7-Source", "mcp-server");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = (int)response.StatusCode;
                    if (errorCode == 429)
                    {
                        return "Rate limited due to too many requests. Please try again later.";
                    }
                    return $"Failed to fetch documentation. Please try again later. Error code: {errorCode}";
                }

                var text = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(text) || text == "No content available" || text == "No context data available")
                {
                    return null;
                }

                return text;
            }
            catch (Exception ex)
            {
                return $"Error fetching library documentation. Please try again later. {ex.Message}";
            }
        }
    }
} 