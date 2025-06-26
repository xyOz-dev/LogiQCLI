using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Presentation.Console.Components.Objects;
using Spectre.Console;

namespace LogiQCLI.Commands.Configuration
{
    [CommandMetadata("Configuration", Tags = new[] { "config", "essential" })]
    public class SettingsCommand : ICommand
    {
        private readonly ApplicationSettings _settings;
        private readonly ConfigurationService _configService;

        public SettingsCommand(ApplicationSettings settings, ConfigurationService configService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "settings",
                Description = "Display current application settings. Use '/settings interactive' for interactive configuration mode."
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(args) && args.Trim().ToLowerInvariant() == "interactive")
                {
                    return Task.FromResult(HandleInteractiveMode());
                }
                else
                {
                    return Task.FromResult(DisplayCurrentSettings());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error executing settings command: {ex.Message}[/]");
            }
        }

        private string DisplayCurrentSettings()
        {

            var settingsData = new Dictionary<string, string>
            {
                { "User Data Path", _settings.UserDataPath ?? "[dim]Not set[/]" },
                { "Active API Key", _settings.ActiveApiKeyNickname ?? "[dim]Not set[/]" },
                { "Workspace", _settings.Workspace ?? "[dim]Not set[/]" },
                { "Default Model", _settings.DefaultModel ?? "[dim]Not set[/]" },
                { "GitHub Token", !string.IsNullOrEmpty(_settings.GitHub?.Token) ? "Configured" : "[dim]Not set[/]" },
                { "Tavily API Key", !string.IsNullOrEmpty(_settings.Tavily?.ApiKey) ? "Configured" : "[dim]Not set[/]" }
            };

            TableFormatter.RenderKeyValueTable("Current Settings", settingsData, Color.Blue);


            if (_settings.ApiKeys.Any())
            {
                var apiKeyRows = _settings.ApiKeys.Select(key => new ApiKeyTableRow
                {
                    Nickname = key.Nickname,
                    ObfuscatedKey = key.GetObfuscatedKey()
                });

                TableFormatter.RenderApiKeysTable(apiKeyRows, _settings.ActiveApiKeyNickname ?? "");
            }


            var centeredHelp = Align.Center(new Markup("[dim]Use [green]/settings interactive[/] for interactive mode or specific commands:[/]\n" +
                                                      "[dim]  /addkey - Add new API key[/]\n" +
                                                      "[dim]  /switchkey - Switch API keys[/]\n" +
                                                      "[dim]  /model - Change model[/]\n" +
                                                      "[dim]  /workspace - Change workspace[/]"));
            AnsiConsole.Write(centeredHelp);
            AnsiConsole.WriteLine();

            return "";
        }

        private string HandleInteractiveMode()
        {
            var menuOptions = new List<string>
            {
                "View Current Settings",
                "Change Workspace",
                "Change Default Model",
                "Manage API Keys",
                "Configure GitHub",
                "Configure Tavily",
                "Experimental Features",
                "Exit"
            };

            while (true)
            {
                AnsiConsole.Clear();
                
    
                var summaryPanel = new Panel($"[cyan]Current Configuration[/]\n" +
                                           $"Workspace: [yellow]{_settings.Workspace ?? "Not set"}[/]\n" +
                                           $"Model: [yellow]{_settings.DefaultModel ?? "Not set"}[/]\n" +
                                           $"Active API Key: [yellow]{_settings.ActiveApiKeyNickname ?? "Not set"}[/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = Style.Parse("blue"),
                    Header = new PanelHeader("[bold blue]Settings Overview[/]")
                };
                
                var centeredSummary = Align.Center(summaryPanel);
                AnsiConsole.Write(centeredSummary);
                AnsiConsole.WriteLine();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]What would you like to do?[/]")
                        .AddChoices(menuOptions));

                switch (choice)
                {
                    case "View Current Settings":
                        DisplayDetailedSettings();
                        break;
                    case "Change Workspace":
                        HandleWorkspaceChange();
                        break;
                    case "Change Default Model":
                        HandleModelChange();
                        break;
                    case "Manage API Keys":
                        HandleApiKeyManagement();
                        break;
                    case "Configure GitHub":
                        HandleGitHubConfiguration();
                        break;
                    case "Configure Tavily":
                        HandleTavilyConfiguration();
                        break;
                    case "Experimental Features":
                        HandleExperimentalFeatures();
                        break;
                    case "Exit":
                        return "[green]Settings configuration completed.[/]";
                }

                if (choice != "Exit")
                {
                    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
                    Console.ReadKey(true);
                }
            }
        }

        private void DisplayDetailedSettings()
        {
            AnsiConsole.Clear();
            DisplayCurrentSettings();
        }

        private void HandleWorkspaceChange()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan]Change Workspace Directory[/]");
            AnsiConsole.MarkupLine($"[dim]Current: {_settings.Workspace ?? "Not set"}[/]");
            AnsiConsole.WriteLine();

            var newWorkspace = AnsiConsole.Ask<string>("[green]Enter new workspace path:[/]");
            
            if (!string.IsNullOrWhiteSpace(newWorkspace))
            {
                if (Directory.Exists(newWorkspace))
                {
                    _settings.Workspace = newWorkspace;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Workspace updated to: {newWorkspace}[/]");
                }
                else
                {
                    var create = AnsiConsole.Confirm($"[yellow]Directory '{newWorkspace}' does not exist. Create it?[/]");
                    if (create)
                    {
                        try
                        {
                            Directory.CreateDirectory(newWorkspace);
                            _settings.Workspace = newWorkspace;
                            _configService.SaveSettings(_settings);
                            AnsiConsole.MarkupLine($"[green]✓ Directory created and workspace updated to: {newWorkspace}[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗ Failed to create directory: {ex.Message}[/]");
                        }
                    }
                }
            }
        }

        private void HandleModelChange()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan]Change Default Model[/]");
            AnsiConsole.MarkupLine($"[dim]Current: {_settings.DefaultModel ?? "Not set"}[/]");
            AnsiConsole.WriteLine();

            var availableModels = _settings.AvailableModels.ToList();
            availableModels.Add("Enter custom model");

            var modelChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select a model:[/]")
                    .AddChoices(availableModels));

            if (modelChoice == "Enter custom model")
            {
                var customModel = AnsiConsole.Ask<string>("[green]Enter custom model name:[/]");
                if (!string.IsNullOrWhiteSpace(customModel))
                {
                    _settings.DefaultModel = customModel;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Default model updated to: {customModel}[/]");
                }
            }
            else
            {
                _settings.DefaultModel = modelChoice;
                _configService.SaveSettings(_settings);
                AnsiConsole.MarkupLine($"[green]✓ Default model updated to: {modelChoice}[/]");
            }
        }

        private void HandleApiKeyManagement()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[cyan]API Key Management[/]");
                AnsiConsole.WriteLine();

                if (_settings.ApiKeys.Any())
                {
                    var apiKeyRows = _settings.ApiKeys.Select(key => new ApiKeyTableRow
                    {
                        Nickname = key.Nickname,
                        ObfuscatedKey = key.GetObfuscatedKey()
                    });

                    TableFormatter.RenderApiKeysTable(apiKeyRows, _settings.ActiveApiKeyNickname ?? "");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No API keys configured.[/]");
                }

                var apiKeyOptions = new List<string> { "Add New API Key", "Back to Main Menu" };
                
                if (_settings.ApiKeys.Any())
                {
                    apiKeyOptions.Insert(1, "Switch Active API Key");
                    apiKeyOptions.Insert(2, "Remove API Key");
                }

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]What would you like to do?[/]")
                        .AddChoices(apiKeyOptions));

                switch (choice)
                {
                    case "Add New API Key":
                        HandleAddApiKey();
                        break;
                    case "Switch Active API Key":
                        HandleSwitchApiKey();
                        break;
                    case "Remove API Key":
                        HandleRemoveApiKey();
                        break;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        private void HandleAddApiKey()
        {
            var nickname = AnsiConsole.Ask<string>("[green]Enter a nickname for the API key:[/]");
            
            if (_settings.ApiKeys.Any(k => k.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine($"[red]✗ An API key with nickname '{nickname}' already exists.[/]");
                return;
            }

            var apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Enter the API key:[/]")
                    .PromptStyle("green")
                    .Secret());

            _settings.ApiKeys.Add(new ApiKeySettings { Nickname = nickname, ApiKey = apiKey });
            
            if (_settings.ApiKeys.Count == 1)
            {
                _settings.ActiveApiKeyNickname = nickname;
            }

            _configService.SaveSettings(_settings);
            AnsiConsole.MarkupLine($"[green]✓ API key '{nickname}' added successfully.[/]");
        }

        private void HandleSwitchApiKey()
        {
            if (!_settings.ApiKeys.Any()) return;

            var keyNicknames = _settings.ApiKeys.Select(k => k.Nickname).ToList();
            
            var selectedKey = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select API key to activate:[/]")
                    .AddChoices(keyNicknames));

            _settings.ActiveApiKeyNickname = selectedKey;
            _configService.SaveSettings(_settings);
            AnsiConsole.MarkupLine($"[green]✓ Switched to API key: {selectedKey}[/]");
        }

        private void HandleRemoveApiKey()
        {
            if (!_settings.ApiKeys.Any()) return;

            var keyNicknames = _settings.ApiKeys.Select(k => k.Nickname).ToList();
            
            var selectedKey = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[red]Select API key to remove:[/]")
                    .AddChoices(keyNicknames));

            var confirm = AnsiConsole.Confirm($"[red]Are you sure you want to remove '{selectedKey}'?[/]");
            
            if (confirm)
            {
                _settings.ApiKeys.RemoveAll(k => k.Nickname == selectedKey);
                
                if (_settings.ActiveApiKeyNickname == selectedKey)
                {
                    _settings.ActiveApiKeyNickname = _settings.ApiKeys.FirstOrDefault()?.Nickname;
                }

                _configService.SaveSettings(_settings);
                AnsiConsole.MarkupLine($"[green]✓ API key '{selectedKey}' removed successfully.[/]");
            }
        }

        private void HandleGitHubConfiguration()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan]GitHub Integration Configuration[/]");
            AnsiConsole.WriteLine();

            var currentToken = !string.IsNullOrEmpty(_settings.GitHub?.Token) ? "Configured" : "Not set";
            var currentOwner = _settings.GitHub?.DefaultOwner ?? "Not set";
            var currentRepo = _settings.GitHub?.DefaultRepo ?? "Not set";

            AnsiConsole.MarkupLine($"[dim]Current Token: {currentToken}[/]");
            AnsiConsole.MarkupLine($"[dim]Default Owner: {currentOwner}[/]");
            AnsiConsole.MarkupLine($"[dim]Default Repository: {currentRepo}[/]");
            AnsiConsole.WriteLine();

            var options = new List<string>
            {
                "Set Access Token",
                "Set Default Owner",
                "Set Default Repository",
                "Reset to Defaults",
                "Back to Main Menu"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to configure?[/]")
                    .AddChoices(options));

            switch (choice)
            {
                case "Set Access Token":
                    var token = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter GitHub Personal Access Token:[/]")
                            .PromptStyle("green")
                            .Secret()
                            .AllowEmpty());
                    if (_settings.GitHub == null) _settings.GitHub = new GitHubSettings();
                    _settings.GitHub.Token = string.IsNullOrWhiteSpace(token) ? null : token;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ GitHub token {(string.IsNullOrWhiteSpace(token) ? "cleared" : "updated")}.[/]");
                    break;

                case "Set Default Owner":
                    var owner = AnsiConsole.Ask<string>("[green]Enter default GitHub owner/organization (leave empty to clear):[/]", string.Empty);
                    if (_settings.GitHub == null) _settings.GitHub = new GitHubSettings();
                    _settings.GitHub.DefaultOwner = string.IsNullOrWhiteSpace(owner) ? null : owner;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Default owner {(string.IsNullOrWhiteSpace(owner) ? "cleared" : $"set to: {owner}")}.[/]");
                    break;

                case "Set Default Repository":
                    var repo = AnsiConsole.Ask<string>("[green]Enter default repository name (leave empty to clear):[/]", string.Empty);
                    if (_settings.GitHub == null) _settings.GitHub = new GitHubSettings();
                    _settings.GitHub.DefaultRepo = string.IsNullOrWhiteSpace(repo) ? null : repo;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Default repository {(string.IsNullOrWhiteSpace(repo) ? "cleared" : $"set to: {repo}")}.[/]");
                    break;

                case "Reset to Defaults":
                    var confirm = AnsiConsole.Confirm("[red]Reset all GitHub settings to defaults (this will clear your token)?[/]");
                    if (confirm)
                    {
                        _settings.GitHub = new GitHubSettings();
                        _configService.SaveSettings(_settings);
                        AnsiConsole.MarkupLine("[green]✓ GitHub settings reset to defaults.[/]");
                    }
                    break;
            }
        }

        private void HandleTavilyConfiguration()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan]Tavily Search Configuration[/]");
            AnsiConsole.WriteLine();

            var currentApiKey = !string.IsNullOrEmpty(_settings.Tavily?.ApiKey) ? "Configured" : "Not set";
            var currentBaseUrl = _settings.Tavily?.BaseUrl ?? "https://api.tavily.com";
            var currentMaxResults = _settings.Tavily?.DefaultMaxResults ?? 5;
            var currentSearchDepth = _settings.Tavily?.DefaultSearchDepth ?? "basic";

            AnsiConsole.MarkupLine($"[dim]Current API Key: {currentApiKey}[/]");
            AnsiConsole.MarkupLine($"[dim]Current Base URL: {currentBaseUrl}[/]");
            AnsiConsole.MarkupLine($"[dim]Default Max Results: {currentMaxResults}[/]");
            AnsiConsole.MarkupLine($"[dim]Default Search Depth: {currentSearchDepth}[/]");
            AnsiConsole.WriteLine();

            var options = new List<string>
            {
                "Set API Key",
                "Configure Base URL",
                "Set Default Max Results",
                "Set Default Search Depth",
                "Reset to Defaults",
                "Back to Main Menu"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to configure?[/]")
                    .AddChoices(options));

            switch (choice)
            {
                case "Set API Key":
                    var apiKey = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter Tavily API key:[/]")
                            .PromptStyle("green")
                            .Secret()
                            .AllowEmpty());
                    if (_settings.Tavily == null) _settings.Tavily = new TavilySettings();
                    _settings.Tavily.ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Tavily API key {(string.IsNullOrWhiteSpace(apiKey) ? "cleared" : "updated")}.[/]");
                    break;

                case "Configure Base URL":
                    var baseUrl = AnsiConsole.Ask<string>("[green]Enter Tavily base URL:[/]", "https://api.tavily.com");
                    if (_settings.Tavily == null) _settings.Tavily = new TavilySettings();
                    _settings.Tavily.BaseUrl = baseUrl;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Tavily base URL updated to: {baseUrl}[/]");
                    break;

                case "Set Default Max Results":
                    var maxResults = AnsiConsole.Ask<int>("[green]Enter default max results (1-20):[/]", 5);
                    maxResults = Math.Max(1, Math.Min(20, maxResults));
                    if (_settings.Tavily == null) _settings.Tavily = new TavilySettings();
                    _settings.Tavily.DefaultMaxResults = maxResults;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Default max results updated to: {maxResults}[/]");
                    break;

                case "Set Default Search Depth":
                    var searchDepth = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Select default search depth:[/]")
                            .AddChoices("basic", "advanced"));
                    if (_settings.Tavily == null) _settings.Tavily = new TavilySettings();
                    _settings.Tavily.DefaultSearchDepth = searchDepth;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]✓ Default search depth updated to: {searchDepth}[/]");
                    break;

                case "Reset to Defaults":
                    var confirm = AnsiConsole.Confirm("[red]Reset all Tavily settings to defaults (this will clear your API key)?[/]");
                    if (confirm)
                    {
                        _settings.Tavily = new TavilySettings();
                        _configService.SaveSettings(_settings);
                        AnsiConsole.MarkupLine("[green]✓ Tavily settings reset to defaults.[/]");
                    }
                    break;
            }
        }

        private void HandleExperimentalFeatures()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[cyan]Experimental Features[/]");
                AnsiConsole.WriteLine();

                var prompt = new MultiSelectionPrompt<string>()
                    .Title("[green]Toggle features (space to select, enter to save)[/]")
                    .NotRequired();

                // features
                const string dedupLabel = "File-read deduplication";
                prompt.AddChoice(dedupLabel);

                if (_settings.Experimental.DeduplicateFileReads)
                    prompt.Select(dedupLabel);

                var selections = AnsiConsole.Prompt(prompt);

                _settings.Experimental.DeduplicateFileReads = selections.Contains(dedupLabel);
                _configService.SaveSettings(_settings);

                AnsiConsole.MarkupLine("[green]✓ Settings saved. Press any key to go back.[/]");
                Console.ReadKey(true);
                return;
            }
        }
    }
} 
