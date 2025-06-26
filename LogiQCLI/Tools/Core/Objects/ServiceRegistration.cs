using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Objects
{
    public class ServiceRegistration
    {
        public Type ServiceType { get; set; } = null!;
        public Type? ImplementationType { get; set; }
        public Func<IServiceContainer, object>? Factory { get; set; }
        public object? Instance { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }
}
