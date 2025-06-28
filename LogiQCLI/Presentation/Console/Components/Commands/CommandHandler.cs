using System;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class CommandHandler
    {
        private readonly LogiQCLI.Commands.Core.CommandHandler _commandHandler;

        public CommandHandler(LogiQCLI.Commands.Core.CommandHandler commandHandler)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public bool IsCommand(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Trim().StartsWith("/");
        }

        public async Task ExecuteCommand(string userInput)
        {
            try
            {
                var handled = await _commandHandler.TryExecuteCommand(userInput);
                
                if (!handled)
                {
                    var parts = userInput.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    var commandName = parts[0];
                    
                    var availableCommands = _commandHandler.GetAvailableCommandNames();
                    
                    AnsiConsole.MarkupLine($"[red]Unknown command: {commandName}[/]");
                    
                    if (availableCommands.Any())
                    {
                        var formattedCommands = availableCommands.Select(c => $"/{c}").OrderBy(c => c);
                        AnsiConsole.MarkupLine($"[yellow]Available commands: {string.Join(", ", formattedCommands)}[/]");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error executing command: {ex.Message}[/]");
            }
        }
    }
}
