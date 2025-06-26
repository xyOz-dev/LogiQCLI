using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Provider
    {
        [JsonPropertyName("order")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Order { get; set; }

        [JsonPropertyName("allow_fallbacks")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AllowFallbacks { get; set; }

        [JsonPropertyName("require_parameters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? RequireParameters { get; set; }

        [JsonPropertyName("data_collection")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DataCollection { get; set; }

        [JsonPropertyName("only")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Only { get; set; }

        [JsonPropertyName("ignore")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Ignore { get; set; }

        [JsonPropertyName("quantizations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Quantizations { get; set; }

        [JsonPropertyName("sort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Sort { get; set; }

        [JsonPropertyName("max_price")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MaxPrice? MaxPrice { get; set; }
    }
}
