using LogiQCLI.Commands.Core.Objects;
using System;
using System.Collections.Generic;

namespace LogiQCLI.Commands.Core.Interfaces
{
    public interface ICommandRegistry
    {
        void RegisterCommand(CommandTypeInfo commandInfo);
        void RegisterCommand(ICommand commandInstance);
        ICommand? GetCommand(string name);
        CommandTypeInfo? GetCommandInfo(string name);
        List<CommandTypeInfo> GetAllCommands();
        List<CommandTypeInfo> GetCommandsByCategory(string category);
        List<CommandTypeInfo> GetCommandsByTag(string tag);
        List<CommandTypeInfo> QueryCommands(Func<CommandTypeInfo, bool> predicate);
        bool IsCommandRegistered(string name);
    }
} 