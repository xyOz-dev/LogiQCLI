using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Infrastructure.Providers;

namespace LogiQCLI.Core.Services
{
    public sealed class ModelDiscoveryService : IModelDiscoveryService
    {
        public async Task<IReadOnlyList<string>> GetAllModelIdsAsync(ApplicationSettings settings, bool refresh = false, CancellationToken cancellationToken = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var providers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { settings.DefaultProvider };
            foreach (var key in settings.ApiKeys)
                if (!string.IsNullOrWhiteSpace(key.Provider)) providers.Add(key.Provider);

            foreach (var providerName in providers)
            {
                try
                {
                    var container = BuildTransientContainer(settings, providerName);
                    var provider = ProviderFactory.Create(container);
                    var models = await provider.ListModelsAsync(cancellationToken);
                    foreach (var model in models)
                    {
                        var id = model.Id;
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        if (!id.Contains('/')) id = $"{providerName}/{id}";
                        result.Add(id.Trim());
                    }
                }
                catch { }
            }

            if (!result.Any() && !string.IsNullOrWhiteSpace(settings.DefaultModel)) result.Add(settings.DefaultModel);
            return result.ToList();
        }

        private static Core.Services.ServiceContainer BuildTransientContainer(ApplicationSettings settings, string providerName)
        {
            var container = new Core.Services.ServiceContainer();
            var cloned = CloneSettings(settings, providerName);
            container.RegisterInstance(cloned);
            container.RegisterInstance(new HttpClient());
            return container;
        }

        private static ApplicationSettings CloneSettings(ApplicationSettings original, string overrideProvider)
        {
            return new ApplicationSettings
            {
                UserDataPath = original.UserDataPath,
                ActiveApiKeyNickname = original.ActiveApiKeyNickname,
                ApiKeys = original.ApiKeys,
                Workspace = original.Workspace,
                DefaultModel = original.DefaultModel,
                AvailableModels = original.AvailableModels,
                ModeSettings = original.ModeSettings,
                GitHub = original.GitHub,
                Tavily = original.Tavily,
                Experimental = original.Experimental,
                ModelMetadata = original.ModelMetadata,
                CacheStrategy = original.CacheStrategy,
                DefaultProvider = overrideProvider
            };
        }
    }
} 