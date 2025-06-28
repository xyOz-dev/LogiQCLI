using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class ModelListResponse
    {
        [JsonPropertyName("data")]
        public required List<Model> Data { get; set; }
    }
}
