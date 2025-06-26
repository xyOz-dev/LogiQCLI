namespace LogiQCLI.Core.Models.Configuration
{
    public class TavilySettings
    {
        public string? ApiKey { get; set; }
        public string? BaseUrl { get; set; } = "https://api.tavily.com";
        public int DefaultMaxResults { get; set; } = 5;
        public bool DefaultIncludeImages { get; set; } = false;
        public bool DefaultIncludeAnswer { get; set; } = true;
        public string DefaultSearchDepth { get; set; } = "basic";
    }
} 