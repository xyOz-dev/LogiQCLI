using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Objects
{
    public class ToolRegistrationEntry
    {
        public ToolTypeInfo ToolInfo { get; set; } = null!;
        public ITool? Instance { get; set; }
    }
}
