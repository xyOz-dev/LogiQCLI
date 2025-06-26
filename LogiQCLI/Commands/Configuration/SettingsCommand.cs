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
            // Display main settings
            var settingsData = new Dictionary<string, string>
            {
                { "User Data Path", _settings.UserDataPath ?? "[dim]Not set[/]" },
                { "Active API Key", _settings.ActiveApiKeyNickname ?? "[dim]Not set[/]" },
                { "Workspace", _settings.Workspace ?? "[dim]Not set[/]" },
                { "Default Model", _settings.DefaultModel ?? "[dim]Not set[/]" }
            };

            TableFormatter.RenderKeyValueTable("Current Settings", settingsData, Color.Blue);

            // Display API keys if any exist
            if (_settings.ApiKeys.Any())
            {
                var apiKeyRows = _settings.ApiKeys.Select(key => new ApiKeyTableRow
                {
                    Nickname = key.Nickname,
                    ObfuscatedKey = key.GetObfuscatedKey()
                });

                TableFormatter.RenderApiKeysTable(apiKeyRows, _settings.ActiveApiKeyNickname ?? "");
            }

            // Display help information
            var centeredHelp = Align.Center(new Markup("[dim]Use [green]/settings interactive[/] for interactive mode or specific commands:[/]\n" +
                                                      "[dim]  /addkey - Add new API key[/]\n" +
                                                      "[dim]  /switchkey - Switch API keys[/]\n" +
                                                      "[dim]  /model - Change model[/]\n" +
                                                      "[dim]  /workspace - Change workspace[/]"));
            AnsiConsole.Write(centeredHelp);
            AnsiConsole.WriteLine();

            return ""; // Tables are rendered directly
        }

        private string HandleInteractiveMode()
        {
            var menuOptions = new List<string>
            {
                "View Current Settings",
                "Change Workspace",
                "Change Default Model",
                "Manage API Keys",
                "Exit"
            };

            while (true)
            {
                AnsiConsole.Clear();
                
                // Display current settings summary at top
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
    }
} 