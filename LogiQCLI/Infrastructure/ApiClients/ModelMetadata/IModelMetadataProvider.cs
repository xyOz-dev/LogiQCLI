using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.ModelMetadata
{
    public interface IModelMetadataProvider
    {
        string Name { get; }

        Task<ModelEndpointsData> GetModelMetadataAsync(string author, string slug, CancellationToken cancellationToken = default);
    }
} 