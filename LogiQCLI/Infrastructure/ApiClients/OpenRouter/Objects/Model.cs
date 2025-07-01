using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Model
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("pricing")]
        public Pricing? Pricing { get; set; }

        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }

        [JsonPropertyName("architecture")]
        public Architecture? Architecture { get; set; }

        [JsonPropertyName("top_provider")]
        public TopProvider? TopProvider { get; set; }

        [JsonPropertyName("per_request_limits")]
        public PerRequestLimits? PerRequestLimits { get; set; }

        [JsonPropertyName("canonical_slug")]
        public string CanonicalSlug { get; set; } = string.Empty;

        [JsonPropertyName("supported_parameters")]
        public List<string>? SupportedParameters { get; set; }
    }

    public class Architecture
    {
        [JsonPropertyName("input_modalities")]
        public List<string> InputModalities { get; set; } = new();

        [JsonPropertyName("output_modalities")]
        public List<string> OutputModalities { get; set; } = new();

        [JsonPropertyName("tokenizer")]
        public string Tokenizer { get; set; } = string.Empty;

        [JsonPropertyName("instruct_type")]
        public string? InstructType { get; set; }
    }

    public class TopProvider
    {
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }

        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; }

        [JsonPropertyName("is_moderated")]
        public bool? IsModerated { get; set; }

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
