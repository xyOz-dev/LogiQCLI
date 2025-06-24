using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        [JsonConverter(typeof(MessageContentConverter))]
        public object? Content { get; set; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolCallId { get; set; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ToolCall[]? ToolCalls { get; set; }
    }
}