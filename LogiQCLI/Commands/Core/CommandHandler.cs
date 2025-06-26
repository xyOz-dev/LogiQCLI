using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using Spectre.Console;

namespace LogiQCLI.Commands.Core
{
    public class CommandHandler
    {
        private readonly ICommandRegistry _commandRegistry;
        private readonly ICommandFactory? _commandFactory;

        public CommandHandler(ICommandRegistry commandRegistry, ICommandFactory? commandFactory = null)
        {
            _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
            _commandFactory = commandFactory;
        }

        public bool CanHandleCommand(string commandName)
        {
            return _commandRegistry.IsCommandRegistered(commandName);
        }

        public async Task<bool> TryExecuteCommand(string userInput)
        {
            var parts = userInput.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0].TrimStart('/');
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            var command = _commandRegistry.GetCommand(commandName);
            if (command == null && _commandFactory != null)
            {
                var commandInfo = _commandRegistry.GetCommandInfo(commandName);
                if (commandInfo != null && _commandFactory.CanCreateCommand(commandInfo))
                {
                    try
                    {
                        command = _commandFactory.CreateCommand(commandInfo);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error creating command '/{commandName}': {ex.Message}[/]");
                        return true;
                    }
                }
            }

            if (command == null)
            {
                return false;
            }

            try
            {
                var result = await command.Execute(args);
                if (!string.IsNullOrEmpty(result))
                {
                    AnsiConsole.MarkupLine(result);
                }
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error executing command '/{commandName}': {ex.Message}[/]");
                return true;
            }
        }

        public string[] GetAvailableCommandNames()
        {
            var commands = _commandRegistry.GetAllCommands();
            var names = new List<string>();
            
            foreach (var command in commands)
            {
                names.Add(command.Name);
                if (!string.IsNullOrEmpty(command.Alias))
                {
                    names.Add(command.Alias);
                }
            }
            
            return names.ToArray();
        }
    }
} 
