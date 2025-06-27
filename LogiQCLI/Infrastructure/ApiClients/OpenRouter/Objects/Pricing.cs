using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Pricing
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("completion")]
        public string Completion { get; set; } = string.Empty;

        [JsonPropertyName("request")]
        public string Request { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("input_cache_reads")]
        public string? InputCacheReads { get; set; }

        [JsonPropertyName("input_cache_writes")]
        public string? InputCacheWrites { get; set; }
    }
}
