using LogiQCLI.Commands.Core.Objects;
using System;

namespace LogiQCLI.Commands.Core.Interfaces
{
    public interface ICommandFactory
    {
        ICommand CreateCommand(Type commandType);
        ICommand CreateCommand(CommandTypeInfo commandInfo);
        bool CanCreateCommand(Type commandType);
        bool CanCreateCommand(CommandTypeInfo commandInfo);
    }
} 
