using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Interfaces
{
    public interface IToolRegistry
    {
        void RegisterTool(ToolTypeInfo toolInfo);
        void RegisterTool(ITool toolInstance);
        ITool? GetTool(string name);
        ToolTypeInfo? GetToolInfo(string name);
        List<ToolTypeInfo> GetAllTools();
        List<ToolTypeInfo> GetToolsByCategory(string category);
        List<ToolTypeInfo> GetToolsByTag(string tag);
        List<ToolTypeInfo> QueryTools(Func<ToolTypeInfo, bool> predicate);
        bool IsToolRegistered(string name);
    }
}
