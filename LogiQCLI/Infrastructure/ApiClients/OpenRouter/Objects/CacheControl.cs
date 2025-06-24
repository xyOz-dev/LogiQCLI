using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class CacheControl
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "ephemeral";
    }
}