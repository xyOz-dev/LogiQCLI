using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class UsageRequest
    {
        [JsonPropertyName("include")]
        public bool Include { get; set; } = true;

        // Optional hint for shaping (not necessarily sent to provider)
        [JsonIgnore]
        public int MaxCompletionTokens { get; set; } = 1024;
    }
}
