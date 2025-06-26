using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;

namespace LogiQCLI.Commands.Core
{
    public class CommandDiscoveryService : ICommandDiscoveryService
    {
        public List<CommandTypeInfo> DiscoverCommands(Assembly assembly)
        {
            return DiscoverCommands(new[] { assembly });
        }

        public List<CommandTypeInfo> DiscoverCommands(params Assembly[] assemblies)
        {
            var commandTypes = new List<CommandTypeInfo>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t));

                foreach (var type in types)
                {
                    var commandInfo = ExtractCommandInfo(type);
                    if (commandInfo != null)
                    {
                        commandTypes.Add(commandInfo);
                    }
                }
            }

            return commandTypes;
        }

        private CommandTypeInfo? ExtractCommandInfo(Type commandType)
        {
            try
            {
                var metadataAttribute = commandType.GetCustomAttribute<CommandMetadataAttribute>();
                var commandInstance = CreateCommandInstanceForDiscovery(commandType);
                
                if (commandInstance == null && metadataAttribute == null)
                {
                    return null;
                }

                string commandName;
                string category;
                List<string> tags;
                List<string> requiredServices;
                int priority;
                bool requiresWorkspace;
                string? alias;

                if (commandInstance != null)
                {
                    var registeredInfo = commandInstance.GetCommandInfo();
                    commandName = registeredInfo.Name ?? ExtractCommandNameFromType(commandType);
                    category = metadataAttribute?.Category ?? commandInstance.Category;
                    
                    tags = (metadataAttribute?.Tags?.Length > 0) 
                        ? metadataAttribute.Tags.ToList()
                        : new List<string>();
                    
                    requiredServices = (metadataAttribute?.RequiredServices?.Length > 0)
                        ? metadataAttribute.RequiredServices.ToList()
                        : (commandInstance.RequiredServices ?? new List<string>());
                    priority = metadataAttribute?.Priority ?? commandInstance.Priority;
                    requiresWorkspace = metadataAttribute?.RequiresWorkspace ?? commandInstance.RequiresWorkspace;
                    alias = metadataAttribute?.Alias ?? registeredInfo.Alias;
                }
                else
                {
                    commandName = ExtractCommandNameFromType(commandType);
                    category = metadataAttribute?.Category ?? ExtractCategoryFromNamespace(commandType);
                    
                    tags = (metadataAttribute?.Tags?.Length > 0)
                        ? metadataAttribute.Tags.ToList()
                        : new List<string>();
                    
                    requiredServices = (metadataAttribute?.RequiredServices?.Length > 0)
                        ? metadataAttribute.RequiredServices.ToList()
                        : new List<string>();
                    priority = metadataAttribute?.Priority ?? 100;
                    requiresWorkspace = metadataAttribute?.RequiresWorkspace ?? true;
                    alias = metadataAttribute?.Alias;
                }

                return new CommandTypeInfo
                {
                    CommandType = commandType,
                    Name = commandName,
                    Category = category,
                    Tags = tags,
                    RequiredServices = requiredServices,
                    Priority = priority,
                    RequiresWorkspace = requiresWorkspace,
                    Alias = alias
                };
            }
            catch
            {
                return null;
            }
        }

        private string ExtractCommandNameFromType(Type commandType)
        {
            var typeName = commandType.Name;
            if (typeName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
            {
                typeName = typeName.Substring(0, typeName.Length - 7);
            }
            
            return ConvertToCommandName(typeName);
        }

        private string ConvertToCommandName(string typeName)
        {

            var result = string.Empty;
            for (int i = 0; i < typeName.Length; i++)
            {
                if (i > 0 && char.IsUpper(typeName[i]))
                {
                    result += "_";
                }
                result += char.ToLower(typeName[i]);
            }
            return result;
        }

        private string ExtractCategoryFromNamespace(Type commandType)
        {
            var namespaceParts = commandType.Namespace?.Split('.') ?? new string[0];
            if (namespaceParts.Length >= 3)
            {
                return namespaceParts[2];
            }
            return "General";
        }

        private ICommand? CreateCommandInstanceForDiscovery(Type commandType)
        {
            try
            {
                var constructors = commandType.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length)
                    .ToList();

                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    var parameterValues = new object[parameters.Length];

                    bool canCreate = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.HasDefaultValue)
                        {
                            parameterValues[i] = param.DefaultValue!;
                        }
                        else if (param.ParameterType.IsValueType)
                        {
                            parameterValues[i] = Activator.CreateInstance(param.ParameterType)!;
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            parameterValues[i] = string.Empty;
                        }
                        else
                        {
                            parameterValues[i] = null!;
                        }
                    }

                    if (canCreate)
                    {
                        try
                        {
                            return (ICommand)Activator.CreateInstance(commandType, parameterValues)!;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
} 
