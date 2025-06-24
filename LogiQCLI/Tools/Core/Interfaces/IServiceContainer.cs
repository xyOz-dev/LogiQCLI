using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.Core.Interfaces
{
    public interface IServiceContainer
    {
        void RegisterInstance<TService>(TService instance) where TService : class;
        void RegisterFactory<TService>(Func<IServiceContainer, TService> factory) where TService : class;
        void RegisterSingleton<TService, TImplementation>() where TImplementation : TService where TService : class;
        TService GetService<TService>() where TService : class;
        object? GetService(Type serviceType);
        bool IsRegistered<TService>() where TService : class;
        bool IsRegistered(Type serviceType);
    }
}
