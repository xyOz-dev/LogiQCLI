using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LogiQCLI.Core.Services
{
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, ServiceRegistration> _services = new Dictionary<Type, ServiceRegistration>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly HashSet<Type> _resolving = new HashSet<Type>();

        public void RegisterInstance<TService>(TService instance) where TService : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            _lock.EnterWriteLock();
            try
            {
                _services[typeof(TService)] = new ServiceRegistration
                {
                    ServiceType = typeof(TService),
                    Instance = instance,
                    Lifetime = ServiceLifetime.Singleton
                };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RegisterFactory<TService>(Func<IServiceContainer, TService> factory) where TService : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _lock.EnterWriteLock();
            try
            {
                _services[typeof(TService)] = new ServiceRegistration
                {
                    ServiceType = typeof(TService),
                    Factory = container => factory(container),
                    Lifetime = ServiceLifetime.Transient
                };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService where TService : class
        {
            _lock.EnterWriteLock();
            try
            {
                _services[typeof(TService)] = new ServiceRegistration
                {
                    ServiceType = typeof(TService),
                    ImplementationType = typeof(TImplementation),
                    Lifetime = ServiceLifetime.Singleton
                };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public TService GetService<TService>() where TService : class
        {
            return (TService)GetService(typeof(TService))!;
        }

        public object? GetService(Type serviceType)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_services.TryGetValue(serviceType, out var registration))
                {
                    return null;
                }

                return ResolveService(registration);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsRegistered<TService>() where TService : class
        {
            return IsRegistered(typeof(TService));
        }

        public bool IsRegistered(Type serviceType)
        {
            _lock.EnterReadLock();
            try
            {
                return _services.ContainsKey(serviceType);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private object ResolveService(ServiceRegistration registration)
        {
            if (registration.Instance != null)
            {
                return registration.Instance;
            }

            lock (_resolving)
            {
                if (_resolving.Contains(registration.ServiceType))
                {
                    throw new InvalidOperationException($"Circular dependency detected for service {registration.ServiceType.Name}");
                }

                _resolving.Add(registration.ServiceType);
                try
                {
                    if (registration.Factory != null)
                    {
                        var instance = registration.Factory(this);
                        
                        if (registration.Lifetime == ServiceLifetime.Singleton)
                        {
                            registration.Instance = instance;
                        }
                        
                        return instance;
                    }

                    if (registration.ImplementationType != null)
                    {
                        var instance = CreateInstance(registration.ImplementationType);
                        
                        if (registration.Lifetime == ServiceLifetime.Singleton)
                        {
                            registration.Instance = instance;
                        }
                        
                        return instance;
                    }

                    throw new InvalidOperationException($"No way to create instance of {registration.ServiceType.Name}");
                }
                finally
                {
                    _resolving.Remove(registration.ServiceType);
                }
            }
        }

        private object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"No public constructors found for {type.Name}");
            }

            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                var service = GetService(parameterType);
                
                if (service == null && !parameters[i].HasDefaultValue)
                {
                    throw new InvalidOperationException($"Unable to resolve service for type {parameterType.Name}");
                }
                
                parameterInstances[i] = service ?? parameters[i].DefaultValue!;
            }

            return Activator.CreateInstance(type, parameterInstances)!;
        }
    }
}
