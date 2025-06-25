using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;

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
                Description = "Display current application settings (use individual commands to modify)"
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                var output = new StringBuilder();
                output.AppendLine("[cyan]Current Settings:[/]");
                output.AppendLine();
                
                output.AppendLine($"[yellow]User Data Path:[/] {_settings.UserDataPath ?? "[dim]Not set[/]"}");
                output.AppendLine($"[yellow]Active API Key:[/] {_settings.ActiveApiKeyNickname ?? "[dim]Not set[/]"}");
                output.AppendLine($"[yellow]Workspace:[/] {_settings.Workspace ?? "[dim]Not set[/]"}");
                output.AppendLine($"[yellow]Default Model:[/] {_settings.DefaultModel ?? "[dim]Not set[/]"}");
                
                if (_settings.ApiKeys.Any())
                {
                    output.AppendLine();
                    output.AppendLine($"[yellow]Available API Keys ({_settings.ApiKeys.Count}):[/]");
                    foreach (var key in _settings.ApiKeys)
                    {
                        var status = key.Nickname == _settings.ActiveApiKeyNickname ? " [green](active)[/]" : "";
                        output.AppendLine($"  • {key.Nickname} ({key.GetObfuscatedKey()}){status}");
                    }
                }

                output.AppendLine();
                output.AppendLine("[dim]Use specific commands to modify settings:[/]");
                output.AppendLine("[dim]  /addkey - Add new API key[/]");
                output.AppendLine("[dim]  /switchkey - Switch API keys[/]");
                output.AppendLine("[dim]  /model - Change model[/]");
                output.AppendLine("[dim]  /workspace - Change workspace[/]");

                return Task.FromResult(output.ToString());
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error displaying settings: {ex.Message}[/]");
            }
        }
    }
} 