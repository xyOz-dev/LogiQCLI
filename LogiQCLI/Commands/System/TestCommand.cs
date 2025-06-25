using System;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;

namespace LogiQCLI.Commands.System
{
    [CommandMetadata("System", Tags = new[] { "test", "debug" })]
    public class TestCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "test-new",
                Description = "Test command for the new command system"
            };
        }

        public override Task<string> Execute(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return Task.FromResult("[green]✓ New command system is working![/] [dim]Try: /test-new hello world[/]");
            }
            else
            {
                return Task.FromResult($"[green]✓ New command system received:[/] [cyan]{args}[/]");
            }
        }
    }
} 