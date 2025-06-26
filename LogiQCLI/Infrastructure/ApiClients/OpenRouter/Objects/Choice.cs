using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }
}
