using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Interfaces
{
    public interface IToolDiscoveryService
    {
        List<ToolTypeInfo> DiscoverTools(Assembly assembly);
        List<ToolTypeInfo> DiscoverTools(params Assembly[] assemblies);
    }
}
