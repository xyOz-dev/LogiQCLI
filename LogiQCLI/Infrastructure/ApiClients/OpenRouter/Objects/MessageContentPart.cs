using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public abstract class MessageContentPart
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }
}