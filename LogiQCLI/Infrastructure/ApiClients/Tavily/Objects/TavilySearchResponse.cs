using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.Tavily.Objects
{
    public class TavilySearchResponse
    {
        [JsonPropertyName("answer")]
        public string? Answer { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("response_time")]
        public double ResponseTime { get; set; }

        [JsonPropertyName("follow_up_questions")]
        public List<string>? FollowUpQuestions { get; set; }

        [JsonPropertyName("images")]
        public List<TavilyImage>? Images { get; set; }

        [JsonPropertyName("results")]
        public List<TavilySearchResult> Results { get; set; } = new List<TavilySearchResult>();
    }

    public class TavilySearchResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("raw_content")]
        public string? RawContent { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("published_date")]
        public string? PublishedDate { get; set; }
    }

    public class TavilyImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
} 