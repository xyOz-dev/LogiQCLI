using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core.Objects;
using Spectre.Console;

namespace LogiQCLI.Commands.Core
{
    [CommandMetadata("Core", Tags = new[] { "help", "essential" }, Alias = "h")]
    public class HelpCommand : ICommand
    {
        private readonly ICommandRegistry _commandRegistry;

        public HelpCommand(ICommandRegistry commandRegistry)
        {
            _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "help",
                Description = "Show help information for commands (alias: h)",
                Alias = "h",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        command = new
                        {
                            type = "string",
                            description = "Specific command to get help for (optional)"
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
                    return Task.FromResult(ShowAllCommands());
                }
                else
                {
                    return Task.FromResult(ShowCommandHelp(args.Trim()));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error showing help: {ex.Message}[/]");
            }
        }

        private string ShowAllCommands()
        {
            var commands = _commandRegistry.GetAllCommands()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name)
                .ToList();

            if (!commands.Any())
            {
                return "[yellow]No commands available.[/]";
            }

            var output = new StringBuilder();
            output.AppendLine("[cyan]Available Commands (New System):[/]");
            output.AppendLine();

            var categories = commands.GroupBy(c => c.Category).OrderBy(g => g.Key);
            
            foreach (var category in categories)
            {
                output.AppendLine($"[bold yellow]{category.Key}:[/]");
                
                foreach (var command in category.OrderBy(c => c.Name))
                {
                    var alias = !string.IsNullOrEmpty(command.Alias) ? $" (alias: /{command.Alias})" : "";
                    var tags = command.Tags.Any() ? $" [dim]({string.Join(", ", command.Tags)})[/]" : "";
                    
                    output.AppendLine($"  [green]/{command.Name}[/]{alias}{tags}");
                }
                
                output.AppendLine();
            }
            
            return output.ToString();
        }

        private string ShowCommandHelp(string commandName)
        {
            var commandInfo = _commandRegistry.GetCommandInfo(commandName);
            if (commandInfo == null)
            {
                return $"[red]Command '/{commandName}' not found.[/]";
            }

            var output = new StringBuilder();
            output.AppendLine($"[cyan]Command: /{commandInfo.Name}[/]");
            
            if (!string.IsNullOrEmpty(commandInfo.Alias))
            {
                output.AppendLine($"[dim]Alias: /{commandInfo.Alias}[/]");
            }
            
            output.AppendLine($"[dim]Category: {commandInfo.Category}[/]");
            
            if (commandInfo.Tags.Any())
            {
                output.AppendLine($"[dim]Tags: {string.Join(", ", commandInfo.Tags)}[/]");
            }
            
            output.AppendLine($"[dim]Priority: {commandInfo.Priority}[/]");
            output.AppendLine($"[dim]Requires Workspace: {commandInfo.RequiresWorkspace}[/]");
            
            return output.ToString();
        }
    }
} 