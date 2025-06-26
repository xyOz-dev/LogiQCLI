using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.Tavily.Objects
{
    public class TavilySearchRequest
    {
        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("search_depth")]
        public string SearchDepth { get; set; } = "basic";

        [JsonPropertyName("include_images")]
        public bool IncludeImages { get; set; } = false;

        [JsonPropertyName("include_answer")]
        public bool IncludeAnswer { get; set; } = true;

        [JsonPropertyName("include_raw_content")]
        public bool IncludeRawContent { get; set; } = false;

        [JsonPropertyName("max_results")]
        public int MaxResults { get; set; } = 5;

        [JsonPropertyName("include_domains")]
        public List<string>? IncludeDomains { get; set; }

        [JsonPropertyName("exclude_domains")]
        public List<string>? ExcludeDomains { get; set; }
    }
} 