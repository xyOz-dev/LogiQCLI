using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Model
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("pricing")]
        public Pricing Pricing { get; set; }

        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }

        [JsonPropertyName("architecture")]
        public Architecture Architecture { get; set; }

        [JsonPropertyName("top_provider")]
        public TopProvider TopProvider { get; set; }

        [JsonPropertyName("per_request_limits")]
        public PerRequestLimits? PerRequestLimits { get; set; }
    }

    public class Architecture
    {
        [JsonPropertyName("modality")]
        public string Modality { get; set; }

        [JsonPropertyName("tokenizer")]
        public string Tokenizer { get; set; }

        [JsonPropertyName("instruct_type")]
        public string? InstructType { get; set; }
    }

    public class TopProvider
    {
        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; }

        [JsonPropertyName("is_fallback")]
        public bool IsFallback { get; set; }
    }

    public class PerRequestLimits
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }
}