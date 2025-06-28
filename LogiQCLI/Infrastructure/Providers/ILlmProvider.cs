using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.Providers.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.Providers
{
    /// <summary>
    /// Minimal contract that mirrors the canonical OpenAI-style REST endpoints we rely on today.
    /// </summary>
    public interface ILlmProvider
    {
        /// <summary>
        /// Corresponds to POST /chat/completions
        /// </summary>
        Task<ChatCompletionResponse> CreateChatCompletionAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Corresponds to GET /models
        /// </summary>
        Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default);
    }
} 