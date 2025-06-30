using System.Collections.Generic;
using System.Text.Json.Serialization;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenAI.Objects
{
    public class EmbeddingRequest
    {
        // Required
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        // Either string or list of strings/tokens – we use List<string> for simplicity; caller can supply 1 item.
        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = new();

        // Optional parameters
        [JsonPropertyName("encoding_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EncodingFormat { get; set; } // "float" (default) or "base64"

        [JsonPropertyName("dimensions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Dimensions { get; set; }

        [JsonPropertyName("user")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? User { get; set; }
    }

    public class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = new();

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    public class EmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public List<float> Embedding { get; set; } = new();
    }
} 