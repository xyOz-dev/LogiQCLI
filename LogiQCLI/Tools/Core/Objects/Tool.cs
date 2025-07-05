using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Core.Objects
{
    public class Tool
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("function")]
        public Function? Function { get; set; }

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects.CacheControl? CacheControl { get; set; }
    }
}
