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
    public class SwitchKeyCommand : ICommand
    {
        private readonly ApplicationSettings _settings;
        private readonly ConfigurationService _configService;
        private readonly Action _initializeDisplay;

        public SwitchKeyCommand(ApplicationSettings settings, ConfigurationService configService, Action initializeDisplay)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "switchkey",
                Description = "Switch between available API keys"
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                var availableKeys = _settings.ApiKeys;
                if (availableKeys.Count <= 1)
                {
                    return Task.FromResult("[yellow]No other API keys available to switch to.[/]");
                }

                var selection = new SelectionPrompt<string>()
                    .Title("[green]Select an API key to activate[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more keys)[/]");

                foreach (var key in availableKeys)
                {
                    var label = $"{key.Nickname} ({key.GetObfuscatedKey()})";
                    if (key.Nickname == _settings.ActiveApiKeyNickname)
                    {
                        label += " [cyan](current)[/]";
                    }
                    selection.AddChoice(label);
                }

                var selectedKeyLabel = AnsiConsole.Prompt(selection);
                var selectedNickname = selectedKeyLabel.Split(' ')[0];

                if (selectedNickname != _settings.ActiveApiKeyNickname)
                {
                    _settings.ActiveApiKeyNickname = selectedNickname;
                    _configService.SaveSettings(_settings);
                    _initializeDisplay();
                    return Task.FromResult($"[green]API key switched to '{selectedNickname}'.[/]");
                }

                return Task.FromResult(string.Empty);
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error switching API key: {ex.Message}[/]");
            }
        }
    }
} 