using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.LMStudio;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.Providers.Objects;

namespace LogiQCLI.Infrastructure.Providers.LMStudio
{
    public sealed class LMStudioProvider : ILlmProvider
    {
        private readonly LMStudioClient _client;

        public LMStudioProvider(LMStudioClient client)
        {
            _client = client;
        }

        public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            var baseResponse = await _client.Chat(request, cancellationToken);
            return new ChatCompletionResponse
            {
                Id = baseResponse.Id,
                Choices = baseResponse.Choices,
                Usage = baseResponse.Usage
            };
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _client.GetModelsAsync(cancellationToken);
        }
    }
} 