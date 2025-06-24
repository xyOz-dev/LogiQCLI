using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Core.Objects
{
    public class Parameters
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Properties { get; set; }

        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Required { get; set; }
    }
}