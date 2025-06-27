using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class TextContentPart : MessageContentPart
    {
        public override string Type => "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CacheControl? CacheControl { get; set; }
    }
}
