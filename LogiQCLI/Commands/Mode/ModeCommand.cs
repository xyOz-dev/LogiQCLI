using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Presentation.Console.Components.Objects;
using LogiQCLI.Tools.Core.Objects;
using Spectre.Console;

namespace LogiQCLI.Commands.Mode
{
    [CommandMetadata("Mode", Tags = new[] { "mode", "essential" })]
    public class ModeCommand : ICommand
    {
        private readonly IModeManager _modeManager;
        private readonly ChatSession _chatSession;
        private readonly Action _initializeDisplay;

        public ModeCommand(IModeManager modeManager, ChatSession chatSession, Action initializeDisplay)
        {
            _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "mode",
                Description = "Mode management. Usage: /mode [list|current|switch <mode-id>|info <mode-id>]",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        action = new
                        {
                            type = "string",
                            description = "Action to perform: list, current, switch, info"
                        },
                        modeId = new
                        {
                            type = "string",
                            description = "Mode ID for switch/info actions"
                        }
                    }
                }
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    return Task.FromResult(ShowCurrentMode());
                }

                var parts = args.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var subCommand = parts[0].ToLowerInvariant();
                var subArgs = parts.Length > 1 ? parts[1] : string.Empty;

                switch (subCommand)
                {
                    case "list":
                        return Task.FromResult(ShowAvailableModes());
                    case "current":
                        return Task.FromResult(ShowCurrentMode());
                    case "switch":
                        return Task.FromResult(HandleModeSwitch(subArgs));
                    case "info":
                        return Task.FromResult(ShowModeInfo(subArgs));
                    default:
                        return Task.FromResult("[red]Unknown mode subcommand. Use: list, current, switch <mode-id>, info <mode-id>[/]");
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error executing mode command: {ex.Message}[/]");
            }
        }

        private string ShowCurrentMode()
        {
            var currentMode = _modeManager.GetCurrentMode();
            
            var modeData = new Dictionary<string, string>
            {
                { "ID", currentMode.Id },
                { "Name", currentMode.Name },
                { "Description", currentMode.Description },
                { "Type", currentMode.IsBuiltIn ? "Built-in" : "Custom" },
                { "Allowed Tools", currentMode.AllowedTools.Count.ToString() }
            };

            TableFormatter.RenderKeyValueTable("Current Mode", modeData, Color.Green);
            return ""; // Table is rendered directly, no string return needed
        }

        private string ShowAvailableModes()
        {
            var modes = _modeManager.GetAvailableModes();
            var currentMode = _modeManager.GetCurrentMode();
            
            var modeRows = modes.Select(m => new ModeTableRow
            {
                Id = m.Id,
                Name = m.Name,
                IsBuiltIn = m.IsBuiltIn,
                ToolCount = m.AllowedTools.Count
            });

            TableFormatter.RenderModesTable(modeRows, currentMode.Id);
            return ""; // Table is rendered directly, no string return needed
        }

        private string HandleModeSwitch(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                return "[red]Mode ID is required. Usage: /mode switch <mode-id>[/]";
            }

            var targetMode = _modeManager.GetMode(modeId);
            if (targetMode == null)
            {
                return $"[red]Mode '{modeId}' not found. Use '/mode list' to see available modes.[/]";
            }

            var currentMode = _modeManager.GetCurrentMode();
            if (currentMode.Id == modeId)
            {
                return $"[yellow]Already in mode '{targetMode.Name}'.[/]";
            }

            if (_modeManager.SetCurrentMode(modeId))
            {
                _chatSession.UpdateSystemPrompt();
                _initializeDisplay();
                return $"[green]Successfully switched to mode '{targetMode.Name}'.[/]";
            }
            else
            {
                return $"[red]Failed to switch to mode '{modeId}'.[/]";
            }
        }

        private string ShowModeInfo(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                return "[red]Mode ID is required. Usage: /mode info <mode-id>[/]";
            }

            var mode = _modeManager.GetMode(modeId);
            if (mode == null)
            {
                return $"[red]Mode '{modeId}' not found. Use '/mode list' to see available modes.[/]";
            }

            var toolsList = mode.AllowedTools.Count > 0
                ? string.Join(", ", mode.AllowedTools.Take(5)) + (mode.AllowedTools.Count > 5 ? $" (+{mode.AllowedTools.Count - 5} more)" : "")
                : "None";

            var output = new StringBuilder();
            output.AppendLine($"[bold cyan]{mode.Name}[/]");
            output.AppendLine($"[dim]ID:[/] [white]{mode.Id}[/]");
            output.AppendLine($"[dim]Type:[/] [cyan]{(mode.IsBuiltIn ? "Built-in" : "Custom")}[/]");
            output.AppendLine($"[dim]Description:[/]");
            output.AppendLine($"[white]{mode.Description}[/]");
            output.AppendLine($"[dim]Allowed Tools ({mode.AllowedTools.Count}):[/]");
            output.AppendLine($"[yellow]{toolsList}[/]");
            
            if (!string.IsNullOrEmpty(mode.PreferredModel))
            {
                output.AppendLine($"[dim]Preferred Model:[/] [cyan]{mode.PreferredModel}[/]");
            }
            else
            {
                output.AppendLine("[dim]Preferred Model:[/] [grey]Default[/]");
            }

            return output.ToString();
        }
    }
} 