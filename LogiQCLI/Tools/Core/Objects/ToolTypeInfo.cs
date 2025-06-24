using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Objects
{
    public class ToolTypeInfo
    {
        public Type ToolType { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> RequiredServices { get; set; } = new List<string>();
        public int Priority { get; set; } = 100;
        public bool RequiresWorkspace { get; set; } = true;
    }
}
