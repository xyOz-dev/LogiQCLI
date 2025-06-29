using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;


namespace LogiQCLI.Infrastructure.Providers.OpenRouter
{
    public sealed class OpenRouterProvider : ILlmProvider
    {
        private readonly OpenRouterClient _client;

        public string ProviderName => "openrouter";

        public OpenRouterProvider(OpenRouterClient client)
        {
            _client = client;
        }

        public async Task<object> CreateChatCompletionAsync(object request, CancellationToken cancellationToken = default)
        {
            if (request is not ChatRequest chatRequest)
            {
                throw new ArgumentException("OpenRouter provider expects ChatRequest", nameof(request));
            }

            var baseResponse = await _client.Chat(chatRequest, cancellationToken);
            return baseResponse;
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _client.GetModelsAsync(cancellationToken);
        }
    }
} 