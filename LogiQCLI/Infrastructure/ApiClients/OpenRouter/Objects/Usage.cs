using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Usage
    {
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("cost")]
        public double Cost { get; set; }

        [JsonPropertyName("prompt_tokens_details")]
        public PromptTokensDetails? PromptTokensDetails { get; set; }
    }

    public class PromptTokensDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }
}