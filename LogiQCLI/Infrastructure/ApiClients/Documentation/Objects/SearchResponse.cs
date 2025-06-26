using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.Documentation.Objects
{
    public class SearchResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("results")]
        public IReadOnlyList<SearchResult> Results { get; set; } = new List<SearchResult>();
    }
} 