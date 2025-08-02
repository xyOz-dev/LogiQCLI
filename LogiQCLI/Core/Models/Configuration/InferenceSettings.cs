namespace LogiQCLI.Core.Models.Configuration
{
    public class InferenceSettings
    {
        public int MaxCompletionTokens { get; set; } = 1024;
        public double ContextSafetyMarginPct { get; set; } = 0.10; // 10%
        public int MaxToolOutputChars { get; set; } = 100_000;
        public int MaxMessages { get; set; } = 120;
        public bool EnableMiddleOut { get; set; } = true;
    }
}
