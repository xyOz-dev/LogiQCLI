using System;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Presentation.Console.Components;
using Spectre.Console;

namespace LogiQCLI.Commands.Session
{
    [CommandMetadata("Session", Tags = new[] { "essential", "safe" }, Alias = "quit")]
    public class ExitCommand : ICommand
    {
        private readonly InputHandler _inputHandler;

        public ExitCommand(InputHandler inputHandler)
        {
            _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "exit",
                Description = "Exit the application (alias: quit)",
                Alias = "quit"
            };
        }

        public override Task<string> Execute(string args)
        {
            if (_inputHandler.GetConfirmation("Are you sure you want to exit?"))
            {
                AnsiConsole.Clear();
                Environment.Exit(0);
            }
            
            return Task.FromResult(string.Empty);
        }
    }
} 