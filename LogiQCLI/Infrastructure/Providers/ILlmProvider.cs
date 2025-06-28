using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.Providers.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.Providers
{
    public interface ILlmProvider
    {
        Task<ChatCompletionResponse> CreateChatCompletionAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default);
    }
} 