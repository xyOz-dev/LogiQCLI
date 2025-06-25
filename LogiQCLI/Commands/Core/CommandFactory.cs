using System;
using System.Linq;
using System.Reflection;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Commands.Core
{
    public class CommandFactory : ICommandFactory
    {
        private readonly IServiceContainer _serviceContainer;

        public CommandFactory(IServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
        }

        public ICommand CreateCommand(Type commandType)
        {
            if (!typeof(ICommand).IsAssignableFrom(commandType))
            {
                throw new ArgumentException($"Type {commandType.Name} is not assignable to ICommand");
            }

            return CreateCommandInstance(commandType);
        }

        public ICommand CreateCommand(CommandTypeInfo commandInfo)
        {
            if (commandInfo == null) throw new ArgumentNullException(nameof(commandInfo));
            
            return CreateCommandInstance(commandInfo.CommandType);
        }

        public bool CanCreateCommand(Type commandType)
        {
            try
            {
                if (!typeof(ICommand).IsAssignableFrom(commandType))
                {
                    return false;
                }

                var constructor = SelectBestConstructor(commandType);
                if (constructor == null)
                {
                    return false;
                }

                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters)
                {
                    if (!CanResolveParameter(parameter))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CanCreateCommand(CommandTypeInfo commandInfo)
        {
            if (commandInfo == null) return false;

            return CanCreateCommand(commandInfo.CommandType);
        }

        private ICommand CreateCommandInstance(Type commandType)
        {
            var constructor = SelectBestConstructor(commandType);
            if (constructor == null)
            {
                throw new InvalidOperationException($"No suitable constructor found for command type {commandType.Name}");
            }

            var parameters = constructor.GetParameters();
            var parameterValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterValues[i] = ResolveParameter(parameters[i]);
            }

            return (ICommand)Activator.CreateInstance(commandType, parameterValues)!;
        }

        private ConstructorInfo? SelectBestConstructor(Type commandType)
        {
            var constructors = commandType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .ToList();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                bool canUseConstructor = true;

                foreach (var parameter in parameters)
                {
                    if (!CanResolveParameter(parameter))
                    {
                        canUseConstructor = false;
                        break;
                    }
                }

                if (canUseConstructor)
                {
                    return constructor;
                }
            }

            return constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
        }

        private bool CanResolveParameter(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                return true;
            }

            if (_serviceContainer.IsRegistered(parameter.ParameterType))
            {
                return true;
            }

            if (parameter.ParameterType.IsValueType)
            {
                return true;
            }

            if (parameter.ParameterType == typeof(string))
            {
                return true;
            }

            return false;
        }

        private object ResolveParameter(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue!;
            }

            var service = _serviceContainer.GetService(parameter.ParameterType);
            if (service != null)
            {
                return service;
            }

            if (parameter.ParameterType.IsValueType)
            {
                return Activator.CreateInstance(parameter.ParameterType)!;
            }

            if (parameter.ParameterType == typeof(string))
            {
                return string.Empty;
            }

            throw new InvalidOperationException($"Cannot resolve parameter {parameter.Name} of type {parameter.ParameterType.Name}");
        }
    }
} 