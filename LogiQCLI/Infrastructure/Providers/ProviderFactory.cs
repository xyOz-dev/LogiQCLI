using System;
using System.Net.Http;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Tools.Core.Interfaces;
using System.Linq;
using LogiQCLI.Infrastructure.Providers.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.Requesty;
using LogiQCLI.Infrastructure.Providers.Requesty;

namespace LogiQCLI.Infrastructure.Providers
{
    public static class ProviderFactory
    {
        public static ILlmProvider Create(IServiceContainer container)
        {
            var settings = container.GetService<ApplicationSettings>();
            var providerName = (settings?.DefaultProvider ?? "openrouter").ToLowerInvariant();

            switch (providerName)
            {
                case "lmstudio":
                    {
                        var http = container.GetService<HttpClient>() ?? new HttpClient();
                        var baseUrl = Environment.GetEnvironmentVariable("LMSTUDIO_BASE_URL") ?? "http://localhost:1234";
                        var lmClient = new LogiQCLI.Infrastructure.ApiClients.LMStudio.LMStudioClient(http, baseUrl);
                        return new LogiQCLI.Infrastructure.Providers.LMStudio.LMStudioProvider(lmClient);
                    }
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
                                    var reqClient = new RequestyClient(http, key);
            return new RequestyProvider(reqClient);
                    }
                case "openai":
                    {
                        var http = container.GetService<System.Net.Http.HttpClient>() ?? new System.Net.Http.HttpClient();
                        var keyEntry = settings?.ApiKeys.FirstOrDefault(k => k.Provider == "openai" && k.Nickname == settings.ActiveApiKeyNickname)
                                     ?? settings?.ApiKeys.FirstOrDefault(k => k.Provider == "openai");
                        var key = keyEntry?.ApiKey;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            throw new InvalidOperationException("No OpenAI API key found. Use /settings or /addkey to add one for provider 'openai'.");
                        }
                        var baseUrl = settings?.OpenAI?.BaseUrl ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
                        var oaClient = new LogiQCLI.Infrastructure.ApiClients.OpenAI.OpenAiClient(http, key, baseUrl);
                        return new LogiQCLI.Infrastructure.Providers.OpenAI.OpenAIProvider(oaClient);
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