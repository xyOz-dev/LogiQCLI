using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.Providers.Objects;

namespace LogiQCLI.Infrastructure.Providers.Requesty
{
    public sealed class RequestyProvider : ILlmProvider, IDisposable
    {
        private readonly RequestyClient _client;

        public RequestyProvider(RequestyClient client)
        {
            _client = client;
        }

        public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            return await _client.ChatAsync(request, cancellationToken);
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _client.ListModelsAsync(cancellationToken);
        }

        public void Dispose() { }
    }
} 