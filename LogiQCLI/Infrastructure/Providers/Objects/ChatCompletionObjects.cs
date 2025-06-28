using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Infrastructure.Providers.Objects
{
    /// <summary>
    /// Mirrors the JSON schema for POST /chat/completions but lives in a provider-neutral namespace.
    /// Inherits from the existing OpenRouter DTOs so we don't duplicate field definitions.
    /// </summary>
    public class ChatCompletionRequest : ChatRequest { }

    /// <summary>
    /// Response counterpart for /chat/completions.
    /// </summary>
    public class ChatCompletionResponse : ChatResponse { }
} 