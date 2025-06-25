using System;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using Spectre.Console;

namespace LogiQCLI.Commands.Configuration
{
    [CommandMetadata("Configuration", Tags = new[] { "config", "essential" })]
    public class AddKeyCommand : ICommand
    {
        private readonly ApplicationSettings _settings;
        private readonly ConfigurationService _configService;
        private readonly Action _initializeDisplay;

        public AddKeyCommand(ApplicationSettings settings, ConfigurationService configService, Action initializeDisplay)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "addkey",
                Description = "Add a new API key with a nickname for easy switching"
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                AnsiConsole.MarkupLine("[cyan]Add New API Key[/]");
                var nickname = AnsiConsole.Ask<string>("[green]Enter a nickname for the new API key:[/] ");

                if (_settings.ApiKeys.Any(k => k.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult($"[red]An API key with the nickname '{nickname}' already exists.[/]");
                }

                var apiKey = AnsiConsole.Prompt(
                    new TextPrompt<string>($"[green]Enter API key for '{nickname}':[/] ")
                        .PromptStyle("green")
                        .Secret());

                var newKey = new ApiKeySettings { Nickname = nickname, ApiKey = apiKey };
                _settings.ApiKeys.Add(newKey);
                _configService.SaveSettings(_settings);

                var result = $"[green]API key '{nickname}' added successfully.[/]";
                
                if (AnsiConsole.Confirm($"[green]Do you want to make '{nickname}' the active key?[/]", false))
                {
                    _settings.ActiveApiKeyNickname = nickname;
                    _configService.SaveSettings(_settings);
                    _initializeDisplay();
                    result += $"\n[green]API key switched to '{nickname}'.[/]";
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error adding API key: {ex.Message}[/]");
            }
        }
    }
} 