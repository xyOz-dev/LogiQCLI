namespace LogiQCLI.Core.Services
{
    public interface IModelDiscoveryService
    {
        Task<IReadOnlyList<string>> GetAllModelIdsAsync(Models.Configuration.ApplicationSettings settings, bool refresh = false, CancellationToken cancellationToken = default);
    }
} 