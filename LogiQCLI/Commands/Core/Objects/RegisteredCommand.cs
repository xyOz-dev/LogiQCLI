using System;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Core.Objects
{
    public class RegisteredCommand
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Func<string, Task<string>>? Execute { get; set; }
        public Parameters? Parameters { get; set; }
        public string? Alias { get; set; }
    }
} 
