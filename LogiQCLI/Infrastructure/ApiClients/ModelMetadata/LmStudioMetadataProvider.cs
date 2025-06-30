using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.LMStudio;
using LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects;
using LogiQCLI.Infrastructure.ApiClients.ModelMetadata;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.ModelMetadata
{
    public sealed class LmStudioMetadataProvider : IModelMetadataProvider
    {
        private readonly LMStudioClient _client;

        public LmStudioMetadataProvider(LMStudioClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string Name => "lmstudio";

        public async Task<ModelEndpointsData> GetModelMetadataAsync(string author, string slug, CancellationToken cancellationToken = default)
        {
            var combined = $"{author}/{slug}";
            var models = await _client.GetModelsAsync(cancellationToken);
            var model = models.FirstOrDefault(m => string.Equals(m.Id, combined, StringComparison.OrdinalIgnoreCase));

            var model = models.FirstOrDefault(m =>
                string.Equals(m.Id, combined, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Id, slug, StringComparison.OrdinalIgnoreCase));
            if (model == null)
            {
                return new ModelEndpointsData();
            }

            var endpoint = new EndpointInfo
            {
                Name = model.Id,
                ContextLength = model.MaxContextLength,
                Pricing = new Pricing(),
                ProviderName = Name,
                SupportedParameters = new List<string>()
            };

            return new ModelEndpointsData
            {
                Id = model.Id,
                Name = model.Id,
                Description = $"{model.Type} model - {model.Arch} architecture ({model.Quantization})",
                Architecture = new ModelArchitecture
                {
                    InputModalities = model.Type == "vlm" ? new List<string> { "text", "image" } : new List<string> { "text" },
                    Tokenizer = model.Arch,
                    InstructType = null
                },
                Endpoints = new List<EndpointInfo> { endpoint }
            };
        }
    }
} 