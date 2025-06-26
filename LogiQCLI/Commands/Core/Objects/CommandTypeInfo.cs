using System;
using System.Collections.Generic;

namespace LogiQCLI.Commands.Core.Objects
{
    public class CommandTypeInfo
    {
        public Type CommandType { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> RequiredServices { get; set; } = new List<string>();
        public int Priority { get; set; } = 100;
        public bool RequiresWorkspace { get; set; } = true;
        public string? Alias { get; set; }
    }
} 
