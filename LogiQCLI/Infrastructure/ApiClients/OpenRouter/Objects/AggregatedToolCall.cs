namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    internal class AggregatedToolCall
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "function";
        public FunctionCall Function { get; set; } = new();
        public System.Text.StringBuilder ArgumentsBuilder { get; } = new();
    }
}
