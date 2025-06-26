using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Delta
    {
        [JsonPropertyName("role")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ToolCall[]? ToolCalls { get; set; }
    }
}
