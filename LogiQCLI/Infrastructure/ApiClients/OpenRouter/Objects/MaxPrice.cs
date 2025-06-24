using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class MaxPrice
    {
        [JsonPropertyName("prompt")]
        public double? Prompt { get; set; }

        [JsonPropertyName("completion")]
        public double? Completion { get; set; }

        [JsonPropertyName("request")]
        public double? Request { get; set; }

        [JsonPropertyName("image")]
        public double? Image { get; set; }
    }
}