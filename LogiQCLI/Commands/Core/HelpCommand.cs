using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Presentation.Console.Components.Objects;
using Spectre.Console;

namespace LogiQCLI.Commands.Core
{
    [CommandMetadata("Core", Tags = new[] { "help", "essential" }, Alias = "h")]
    public class HelpCommand : ICommand
    {
        private readonly ICommandRegistry _commandRegistry;
        private readonly ICommandFactory _commandFactory;

        public HelpCommand(ICommandRegistry commandRegistry, ICommandFactory commandFactory)
        {
            _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
            _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
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

            var commandRows = commands.Select(c => new CommandTableRow
            {
                Name = c.Name,
                Alias = c.Alias,
                Category = c.Category,
                Description = GetCommandDescription(c.Name),
                Tags = c.Tags.ToList()
            });

            TableFormatter.RenderCommandsTable(commandRows);
            return "";
        }

        private string ShowCommandHelp(string commandName)
        {
            var commandTypeInfo = _commandRegistry.GetCommandInfo(commandName);
            if (commandTypeInfo == null)
            {
                return $"[red]Command '/{commandName}' not found.[/]";
            }

            var output = new StringBuilder();
            output.AppendLine($"[cyan]Command: /{commandTypeInfo.Name}[/]");
            
            if (!string.IsNullOrEmpty(commandTypeInfo.Alias))
            {
                output.AppendLine($"[dim]Alias: /{commandTypeInfo.Alias}[/]");
            }
            
            output.AppendLine($"[dim]Category: {commandTypeInfo.Category}[/]");
            
            if (commandTypeInfo.Tags.Any())
            {
                output.AppendLine($"[dim]Tags: {string.Join(", ", commandTypeInfo.Tags)}[/]");
            }
            
            output.AppendLine($"[dim]Priority: {commandTypeInfo.Priority}[/]");
            output.AppendLine($"[dim]Requires Workspace: {commandTypeInfo.RequiresWorkspace}[/]");
            
            var description = GetCommandDescription(commandName);
            output.AppendLine($"[dim]Description: {description}[/]");
            
            return output.ToString();
        }

        private string GetCommandDescription(string commandName)
        {
            try
            {
                var command = _commandRegistry.GetCommand(commandName);
                if (command != null)
                {
                    var registeredCommand = command.GetCommandInfo();
                    if (!string.IsNullOrEmpty(registeredCommand?.Description))
                    {
                        return registeredCommand.Description;
                    }
                }
                
                var commandTypeInfo = _commandRegistry.GetCommandInfo(commandName);
                if (commandTypeInfo != null && _commandFactory.CanCreateCommand(commandTypeInfo))
                {
                    try
                    {
                        var tempCommand = _commandFactory.CreateCommand(commandTypeInfo);
                        var tempRegisteredCommand = tempCommand.GetCommandInfo();
                        if (!string.IsNullOrEmpty(tempRegisteredCommand?.Description))
                        {
                            return tempRegisteredCommand.Description;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return "No description available";
        }
    }
} 
