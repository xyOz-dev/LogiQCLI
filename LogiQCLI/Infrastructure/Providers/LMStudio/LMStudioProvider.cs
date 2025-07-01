using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.LMStudio;
using LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.Providers.LMStudio
{
    public sealed class LMStudioProvider : ILlmProvider
    {
        private readonly LMStudioClient _client;

        public string ProviderName => "lmstudio";

        public LMStudioProvider(LMStudioClient client)
        {
            _client = client;
        }

        public async Task<object> CreateChatCompletionAsync(object request, CancellationToken cancellationToken = default)
        {
            if (request is not ChatRequest chatRequest)
            {
                throw new ArgumentException("LMStudio provider expects ChatRequest", nameof(request));
            }

            var baseResponse = await _client.Chat(chatRequest, cancellationToken);
            return baseResponse;
        }



        public async Task<IReadOnlyList<Model>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            var lmStudioModels = await _client.GetModelsAsync(cancellationToken);
            return lmStudioModels.Select(ConvertToModel).ToList().AsReadOnly();
        }

        private static Model ConvertToModel(LMStudioModel lmStudioModel)
        {
            return new Model
            {
                Id = lmStudioModel.Id,
                Name = lmStudioModel.Id,
                Description = $"{lmStudioModel.Type} model - {lmStudioModel.Arch} architecture ({lmStudioModel.Quantization})",
                ContextLength = lmStudioModel.MaxContextLength,
                Pricing = null,
                Architecture = new Architecture
                {
                    InputModalities = lmStudioModel.Type == "vlm" ? new List<string> { "text", "image" } : new List<string> { "text" },
                    OutputModalities = new List<string> { "text" },
                    Tokenizer = lmStudioModel.Arch,
                    InstructType = null
                }
            };
        }
    }
} 