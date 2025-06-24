using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Interfaces
{
    public interface IToolFactory
    {
        ITool CreateTool(Type toolType);
        ITool CreateTool(ToolTypeInfo toolInfo);
        bool CanCreateTool(Type toolType);
        bool CanCreateTool(ToolTypeInfo toolInfo);
    }
}
