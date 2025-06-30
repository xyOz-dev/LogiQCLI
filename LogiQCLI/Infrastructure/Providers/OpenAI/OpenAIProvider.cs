using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenAI;
using LogiQCLI.Infrastructure.ApiClients.OpenAI.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.Providers.OpenAI
{
    public sealed class OpenAIProvider : LogiQCLI.Infrastructure.Providers.ILlmProvider
    {
        private readonly OpenAiClient _client;
        public string ProviderName => "openai";

        public OpenAIProvider(OpenAiClient client)
        {
            _client = client;
        }

        public async Task<object> CreateChatCompletionAsync(object request, CancellationToken cancellationToken = default)
        {
            if (request is not ChatRequest chatRequest)
            {
                throw new System.ArgumentException("OpenAI provider expects ChatRequest", nameof(request));
            }

            var response = await _client.ChatAsync(chatRequest, cancellationToken);
            return response;
        }

        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            var openAiModels = await _client.ListModelsAsync(cancellationToken);
            return openAiModels.Select(ConvertToModel).ToList().AsReadOnly();
        }

        private static Model ConvertToModel(OpenAiModel src)
        {
            return new Model
            {
                Id = src.Id,
                Name = src.Id,
                Description = src.OwnedBy,
                ContextLength = src.ContextLength ?? 0,
                Pricing = null,
                Architecture = new Architecture
                {
                    Modality = "text",
                    Tokenizer = string.Empty,
                    InstructType = null
                }
            };
        }
    }
} 