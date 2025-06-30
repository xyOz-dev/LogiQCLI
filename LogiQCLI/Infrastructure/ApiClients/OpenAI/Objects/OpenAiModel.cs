using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenAI.Objects
{
    public class OpenAiModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = string.Empty;

        // Some models include context length in metadata extensions; optional
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
    }

    public class OpenAiModelListResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiModel> Data { get; set; } = new List<OpenAiModel>();
    }
} 