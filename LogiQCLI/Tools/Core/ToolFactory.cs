using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Core.Services
{
    public class ToolFactory : IToolFactory
    {
        private readonly IServiceContainer _serviceContainer;

        public ToolFactory(IServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
        }

        public ITool CreateTool(Type toolType)
        {
            if (!typeof(ITool).IsAssignableFrom(toolType))
            {
                throw new ArgumentException($"Type {toolType.Name} is not assignable to ITool");
            }

            return CreateToolInstance(toolType);
        }

        public ITool CreateTool(ToolTypeInfo toolInfo)
        {
            if (toolInfo == null) throw new ArgumentNullException(nameof(toolInfo));
            
            return CreateToolInstance(toolInfo.ToolType);
        }

        public bool CanCreateTool(Type toolType)
        {
            try
            {
                if (!typeof(ITool).IsAssignableFrom(toolType))
                {
                    return false;
                }

                var constructor = SelectBestConstructor(toolType);
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

        public bool CanCreateTool(ToolTypeInfo toolInfo)
        {
            if (toolInfo == null) return false;

            return CanCreateTool(toolInfo.ToolType);
        }

        private ITool CreateToolInstance(Type toolType)
        {
            var constructor = SelectBestConstructor(toolType);
            if (constructor == null)
            {
                throw new InvalidOperationException($"No suitable constructor found for tool type {toolType.Name}");
            }

            var parameters = constructor.GetParameters();
            var parameterValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterValues[i] = ResolveParameter(parameters[i]);
            }

            return (ITool)Activator.CreateInstance(toolType, parameterValues)!;
        }

        private ConstructorInfo? SelectBestConstructor(Type toolType)
        {
            var constructors = toolType.GetConstructors()
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
            if (_serviceContainer.IsRegistered(parameter.ParameterType))
            {
                var service = _serviceContainer.GetService(parameter.ParameterType);
                if (service != null)
                {
                    return service;
                }
            }

            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue!;
            }

            if (parameter.ParameterType.IsValueType)
            {
                return Activator.CreateInstance(parameter.ParameterType)!;
            }

            if (parameter.ParameterType == typeof(string))
            {
                return string.Empty;
            }

            throw new InvalidOperationException($"Cannot resolve parameter '{parameter.Name}' of type '{parameter.ParameterType.Name}'");
        }
    }
}
