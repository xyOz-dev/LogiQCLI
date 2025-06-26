using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Core.Objects
{
    public class Function
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("parameters")]
        public Parameters? Parameters { get; set; }
    }
}
