using System;
using System.Collections.Generic;

namespace LogiQCLI.Commands.Core.Objects
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CommandMetadataAttribute : Attribute
    {
        public string? Category { get; set; }
        
        public string[] Tags { get; set; } = Array.Empty<string>();
        
        public string[] RequiredServices { get; set; } = Array.Empty<string>();
        
        public int Priority { get; set; } = 100;
        
        public bool RequiresWorkspace { get; set; } = true;
        
        public string? Alias { get; set; }
        
        public CommandMetadataAttribute()
        {

        }
        
        public CommandMetadataAttribute(string category)
        {
            Category = category;
        }
    }
} 
