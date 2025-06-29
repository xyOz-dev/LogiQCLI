using System;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.ModelMetadata;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

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

        public Task<ModelEndpointsData> GetModelMetadataAsync(string author, string slug, CancellationToken cancellationToken = default)
        {
            return _client.GetModelEndpointsAsync(author, slug, cancellationToken);
        }
    }
} 