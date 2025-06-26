using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Web.Objects
{
    public class TavilySearchArguments
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("search_depth")]
        public string? SearchDepth { get; set; }

        [JsonPropertyName("max_results")]
        public int? MaxResults { get; set; }

        [JsonPropertyName("include_answer")]
        public bool? IncludeAnswer { get; set; }

        [JsonPropertyName("include_images")]
        public bool? IncludeImages { get; set; }

        [JsonPropertyName("include_raw_content")]
        public bool? IncludeRawContent { get; set; }

        [JsonPropertyName("include_domains")]
        public List<string>? IncludeDomains { get; set; }

        [JsonPropertyName("exclude_domains")]
        public List<string>? ExcludeDomains { get; set; }
    }
} 