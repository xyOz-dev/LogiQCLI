using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class JsonSchema
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("strict")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Strict { get; set; }

        [JsonPropertyName("schema")]
        public object? Schema { get; set; }
    }
}
