using LogiQCLI.Commands.Core.Objects;
using System.Collections.Generic;
using System.Reflection;

namespace LogiQCLI.Commands.Core.Interfaces
{
    public interface ICommandDiscoveryService
    {
        List<CommandTypeInfo> DiscoverCommands(Assembly assembly);
        List<CommandTypeInfo> DiscoverCommands(params Assembly[] assemblies);
    }
} 
