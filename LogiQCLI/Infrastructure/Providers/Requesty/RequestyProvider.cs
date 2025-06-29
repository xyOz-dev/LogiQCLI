using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.Requesty;

namespace LogiQCLI.Infrastructure.Providers.Requesty
{
    public sealed class RequestyProvider : ILlmProvider, IDisposable
    {
        private readonly RequestyClient _client;

        public string ProviderName => "requesty";

        public RequestyProvider(RequestyClient client)
        {
            _client = client;
        }

        public async Task<object> CreateChatCompletionAsync(object request, CancellationToken cancellationToken = default)
        {
            if (request is not ChatRequest chatRequest)
            {
                throw new ArgumentException("Requesty provider expects ChatRequest", nameof(request));
            }

            return await _client.ChatAsync(chatRequest, cancellationToken);
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _client.ListModelsAsync(cancellationToken);
        }

        public void Dispose() { }
    }
} 