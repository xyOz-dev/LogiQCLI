using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class ChatResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }
}
