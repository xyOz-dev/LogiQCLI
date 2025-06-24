using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class UsageRequest
    {
        [JsonPropertyName("include")]
        public bool Include { get; set; } = true;
    }
}