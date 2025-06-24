using System;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Objects
{
    public class RegisteredTool
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Func<string, Task<string>>? Execute { get; set; }
        public Parameters? Parameters { get; set; }
    }
}
