using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
    }
}
