using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects
{
    public class LMStudioChatResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public List<LMStudioChoice> Choices { get; set; } = new List<LMStudioChoice>();

        [JsonPropertyName("usage")]
        public LMStudioUsage Usage { get; set; } = new LMStudioUsage();

        [JsonPropertyName("stats")]
        public LMStudioStats Stats { get; set; } = new LMStudioStats();

        [JsonPropertyName("model_info")]
        public LMStudioModelInfo? ModelInfo { get; set; }

        [JsonPropertyName("runtime")]
        public LMStudioRuntime? Runtime { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; } = string.Empty;
    }

    public class LMStudioChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("logprobs")]
        public object? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public LMStudioMessage Message { get; set; } = new LMStudioMessage();
    }

    public class LMStudioMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class LMStudioUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class LMStudioStats
    {
        [JsonPropertyName("tokens_per_second")]
        public double? TokensPerSecond { get; set; }

        [JsonPropertyName("time_to_first_token")]
        public double? TimeToFirstToken { get; set; }

        [JsonPropertyName("generation_time")]
        public double? GenerationTime { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }
    }

    public class LMStudioModelInfo
    {
        [JsonPropertyName("arch")]
        public string Arch { get; set; } = string.Empty;

        [JsonPropertyName("quant")]
        public string Quant { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }
    }

    public class LMStudioRuntime
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("supported_formats")]
        public List<string> SupportedFormats { get; set; } = new List<string>();
    }
} 