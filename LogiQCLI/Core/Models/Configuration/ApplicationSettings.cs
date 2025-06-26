using System.Collections.Generic;
using System.Linq;
using LogiQCLI.Core.Models.Modes;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;

namespace LogiQCLI.Core.Models.Configuration
{
    public class ApplicationSettings
    {
        public string? UserDataPath { get; set; }
        public string? ActiveApiKeyNickname { get; set; }
        public List<ApiKeySettings> ApiKeys { get; set; } = new List<ApiKeySettings>();
        public string? Workspace { get; set; }
        public string? DefaultModel { get; set; }
        public List<string> AvailableModels { get; set; } = new List<string>
        {
            "google/gemini-2.5-pro",
            "anthropic/claude-sonnet-4",
            "anthropic/claude-opus-4",
            "anthropic/claude-4-opus-20250522",
            "openai/gpt-4.1",
            "mistralai/devstral-small-2505",
            "inception/mercury-coder-small-beta"
        };
        public ModeSettings ModeSettings { get; set; } = new ModeSettings();
        public GitHubSettings GitHub { get; set; } = new GitHubSettings();
        public TavilySettings Tavily { get; set; } = new TavilySettings();
        






        public CacheStrategy CacheStrategy { get; set; } = CacheStrategy.Auto;

        public ApiKeySettings? GetActiveApiKey() => ApiKeys.FirstOrDefault(k => k.Nickname == ActiveApiKeyNickname);
    }
}
