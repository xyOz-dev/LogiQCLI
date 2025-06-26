using LogiQCLI.Commands.Core.Interfaces;

namespace LogiQCLI.Commands.Core.Objects
{
    public class CommandRegistrationEntry
    {
        public CommandTypeInfo CommandInfo { get; set; } = null!;
        public ICommand? Instance { get; set; }
    }
} 
