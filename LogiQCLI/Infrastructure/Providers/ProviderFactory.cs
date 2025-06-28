using System;
using System.Net.Http;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Tools.Core.Interfaces;
using System.Linq;
using LogiQCLI.Infrastructure.Providers.OpenRouter;
using LogiQCLI.Infrastructure.Providers.Requesty;

namespace LogiQCLI.Infrastructure.Providers
{
    /// <summary>
    /// Central place to create ILlmProvider instances based on environment variables.
    /// For now we only support "openrouter" (default) and "requesty".
    /// </summary>
    public static class ProviderFactory
    {
        public static ILlmProvider Create(IServiceContainer container)
        {
            var settings = container.GetService<ApplicationSettings>();
            var providerName = (settings?.DefaultProvider ?? "openrouter").ToLowerInvariant();

            switch (providerName)
            {
                case "requesty":
                    {
                        var http = container.GetService<HttpClient>() ?? new HttpClient();
                        var keyEntry = settings?.ApiKeys.FirstOrDefault(k => k.Provider == "requesty" && k.Nickname == settings.ActiveApiKeyNickname)
                                     ?? settings?.ApiKeys.FirstOrDefault(k => k.Provider == "requesty");
                        var key = keyEntry?.ApiKey;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            throw new InvalidOperationException("No Requesty API key found. Use /settings or /addkey to add one for provider 'requesty'.");
                        }
                        return new Infrastructure.Providers.Requesty.RequestyProvider(http, key);
                    }
                case "openrouter":
                default:
                    {
                        var openRouterClient = container.GetService<OpenRouterClient>();
                        if (openRouterClient == null)
                        {
                            var http = container.GetService<HttpClient>() ?? new HttpClient();
                            var keyEntry = settings?.ApiKeys.FirstOrDefault(k => k.Provider == "openrouter" && k.Nickname == settings.ActiveApiKeyNickname)
                                         ?? settings?.ApiKeys.FirstOrDefault(k => k.Provider == "openrouter");
                            var key = keyEntry?.ApiKey;
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw new InvalidOperationException("No OpenRouter API key found. Use /settings or /addkey to add one for provider 'openrouter'.");
                            }
                            openRouterClient = new OpenRouterClient(http, key, settings?.CacheStrategy ?? CacheStrategy.Auto);
                        }
                        return new Infrastructure.Providers.OpenRouter.OpenRouterProvider(openRouterClient);
                    }
            }
        }
    }
} 