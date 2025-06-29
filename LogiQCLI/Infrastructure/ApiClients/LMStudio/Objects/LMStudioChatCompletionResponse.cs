using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects
{
    public class LMStudioChatCompletionResponse : ChatResponse
    {
        public LMStudioStats? Stats { get; set; }
        public LMStudioModelInfo? ModelInfo { get; set; }
        public LMStudioRuntime? Runtime { get; set; }
        public string? SystemFingerprint { get; set; }
    }
} 