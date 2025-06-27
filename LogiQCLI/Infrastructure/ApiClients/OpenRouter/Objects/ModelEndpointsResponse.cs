using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class ModelEndpointsResponse
    {
        [JsonPropertyName("data")]
        public ModelEndpointsData Data { get; set; } = new ModelEndpointsData();
    }

    public class ModelEndpointsData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public double Created { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("architecture")]
        public ModelArchitecture Architecture { get; set; } = new ModelArchitecture();

        [JsonPropertyName("endpoints")]
        public List<EndpointInfo> Endpoints { get; set; } = new List<EndpointInfo>();
    }

    public class ModelArchitecture
    {
        [JsonPropertyName("input_modalities")]
        public List<string> InputModalities { get; set; } = new List<string>();

        [JsonPropertyName("output_modalities")]
        public List<string> OutputModalities { get; set; } = new List<string>();

        [JsonPropertyName("tokenizer")]
        public string Tokenizer { get; set; } = string.Empty;

        [JsonPropertyName("instruct_type")]
        public string? InstructType { get; set; }
    }

    public class EndpointInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }

        [JsonPropertyName("pricing")]
        public Pricing Pricing { get; set; } = new Pricing();

        [JsonPropertyName("provider_name")]
        public string ProviderName { get; set; } = string.Empty;

        [JsonPropertyName("supported_parameters")]
        public List<string> SupportedParameters { get; set; } = new List<string>();
    }
} 