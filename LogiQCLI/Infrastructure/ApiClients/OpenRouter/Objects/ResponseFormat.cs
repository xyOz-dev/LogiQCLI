using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class ResponseFormat
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("json_schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchema? JsonSchema { get; set; }
    }
}