using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Core.Services;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Infrastructure.ApiClients.ModelMetadata;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter
{
    public class ModelMetadataService
    {
        private readonly Dictionary<string, IModelMetadataProvider> _providers;
        private readonly ConfigurationService _configService;
        private readonly ApplicationSettings _settings;
        private readonly Dictionary<string, ModelEndpointsData> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _sync = new(1, 1);

        public ModelMetadataService(IEnumerable<IModelMetadataProvider> providers,
                                     ConfigurationService configService,
                                     ApplicationSettings settings)
        {
            _providers = providers?.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase)
                         ?? throw new ArgumentNullException(nameof(providers));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (_settings.ModelMetadata != null)
            {
                foreach (var kv in _settings.ModelMetadata)
                {
                    _cache[kv.Key] = kv.Value;
                }
            }
        }

        private static string BuildKey(string author, string slug) => $"{author}/{slug}".ToLowerInvariant();

        public async Task<ModelEndpointsData> GetModelMetadataAsync(string author, string slug, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var key = BuildKey(author, slug);

            if (!forceRefresh && _cache.TryGetValue(key, out var cached))
                return cached;

            await _sync.WaitAsync(cancellationToken);
            try
            {
                if (!forceRefresh && _cache.TryGetValue(key, out cached))
                    return cached;

                var providerName = (_settings.DefaultProvider ?? "openrouter").ToLowerInvariant();
                if (!_providers.TryGetValue(providerName, out var provider))
                {
                    throw new InvalidOperationException($"No metadata provider registered for '{providerName}'.");
                }

                var fresh = await provider.GetModelMetadataAsync(author, slug, cancellationToken);
                _cache[key] = fresh;
                _settings.ModelMetadata[key] = fresh;
                _configService.SaveSettings(_settings);
                return fresh;
            }
            finally
            {
                _sync.Release();
            }
        }

        public EndpointInfo? GetBestEndpoint(ModelEndpointsData? metadata)
        {
            return metadata?.Endpoints?.OrderByDescending(e => e.ContextLength).FirstOrDefault();
        }
    }
} 