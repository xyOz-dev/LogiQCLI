using System;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Presentation.Console.Session;

namespace LogiQCLI.Commands.Session
{
    [CommandMetadata("Session", Tags = new[] { "essential", "safe" })]
    public class ClearCommand : ICommand
    {
        private readonly ChatSession _chatSession;
        private readonly Action _initializeDisplay;

        public ClearCommand(ChatSession chatSession, Action initializeDisplay)
        {
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "clear",
                Description = "Clear the chat history and reset the display",
    
            };
        }

        public override Task<string> Execute(string args)
        {
            _chatSession.ClearHistory();
            _initializeDisplay();
            return Task.FromResult("[bold green]Chat history has been cleared.[/]");
        }
    }
} 
