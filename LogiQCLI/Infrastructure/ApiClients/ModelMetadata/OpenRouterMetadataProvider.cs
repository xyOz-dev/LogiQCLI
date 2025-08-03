using System;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.ModelMetadata;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using System.Linq;
using System.Collections.Generic;

namespace LogiQCLI.Infrastructure.ApiClients.ModelMetadata
{
    public sealed class OpenRouterMetadataProvider : IModelMetadataProvider
    {
        private readonly OpenRouterClient _client;

        public OpenRouterMetadataProvider(OpenRouterClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string Name => "openrouter";

        public async Task<ModelEndpointsData> GetModelMetadataAsync(string author, string slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _client.GetModelEndpointsAsync(author, slug, cancellationToken);
                if (data?.Endpoints != null && data.Endpoints.Count > 0 && data.Endpoints.Any(e => e.ContextLength > 0))
                {
                    return data;
                }
            }
            catch
            {
            }

            var models = await _client.GetModelsAsync(cancellationToken);
            var id = $"{author}/{slug}";
            var model = models.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase) ||
                                                  string.Equals(m.CanonicalSlug, id, StringComparison.OrdinalIgnoreCase) ||
                                                  string.Equals(m.Name, id, StringComparison.OrdinalIgnoreCase))
                        ?? models.FirstOrDefault(m => m.Id.EndsWith($"/{slug}", StringComparison.OrdinalIgnoreCase) ||
                                                     (m.CanonicalSlug?.EndsWith($"/{slug}", StringComparison.OrdinalIgnoreCase) ?? false));
            if (model == null)
            {
                return new ModelEndpointsData();
            }

            var endpoint = new EndpointInfo
            {
                Name = model.Id,
                ContextLength = model.TopProvider?.ContextLength ?? model.ContextLength,
                Pricing = model.Pricing ?? new Pricing(),
                ProviderName = Name,
                SupportedParameters = model.SupportedParameters ?? new List<string>()
            };

            return new ModelEndpointsData
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Architecture = new ModelArchitecture
                {
                    InputModalities = model.Architecture?.InputModalities ?? new List<string> { "text" },
                    OutputModalities = model.Architecture?.OutputModalities ?? new List<string> { "text" },
                    Tokenizer = model.Architecture?.Tokenizer ?? string.Empty,
                    InstructType = model.Architecture?.InstructType
                },
                Endpoints = new List<EndpointInfo> { endpoint }
            };
        }
    }
} 