using System.Text.Json.Serialization;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("messages")]
        public Message[]? Messages { get; set; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Tool[]? Tools { get; set; }

        [JsonPropertyName("tool_choice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolChoice { get; set; }

        [JsonPropertyName("stream")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Stream { get; set; }

        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResponseFormat? ResponseFormat { get; set; }

        [JsonPropertyName("provider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Provider? Provider { get; set; }

        [JsonPropertyName("models")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Models { get; set; }

        [JsonPropertyName("max_price")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MaxPrice { get; set; }

        [JsonPropertyName("usage")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UsageRequest? Usage { get; set; }

        [JsonPropertyName("requesty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RequestyOptions? Requesty { get; set; }
    }
}
