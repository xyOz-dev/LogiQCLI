using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Core.Services
{
    public class ToolDiscoveryService : IToolDiscoveryService
    {
        public List<ToolTypeInfo> DiscoverTools(Assembly assembly)
        {
            return DiscoverTools(new[] { assembly });
        }

        public List<ToolTypeInfo> DiscoverTools(params Assembly[] assemblies)
        {
            var toolTypes = new List<ToolTypeInfo>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ITool).IsAssignableFrom(t));

                foreach (var type in types)
                {
                    var toolInfo = ExtractToolInfo(type);
                    if (toolInfo != null)
                    {
                        toolTypes.Add(toolInfo);
                    }
                }
            }

            return toolTypes;
        }

        private ToolTypeInfo? ExtractToolInfo(Type toolType)
        {
            try
            {
                var metadataAttribute = toolType.GetCustomAttribute<ToolMetadataAttribute>();
                var toolInstance = CreateToolInstanceForDiscovery(toolType);
                
                if (toolInstance == null && metadataAttribute == null)
                {
                    return null;
                }

                string toolName;
                string category;
                List<string> tags;
                List<string> requiredServices;
                int priority;
                bool requiresWorkspace;

                if (toolInstance != null)
                {
                    var registeredInfo = toolInstance.GetToolInfo();
                    toolName = registeredInfo.Name;
                    category = metadataAttribute?.Category ?? toolInstance.Category;
                    
                    tags = (metadataAttribute?.Tags?.Length > 0) 
                        ? metadataAttribute.Tags.ToList()
                        : new List<string>();
                    
                    requiredServices = (metadataAttribute?.RequiredServices?.Length > 0)
                        ? metadataAttribute.RequiredServices.ToList()
                        : (toolInstance.RequiredServices ?? new List<string>());
                    priority = metadataAttribute?.Priority ?? toolInstance.Priority;
                    requiresWorkspace = metadataAttribute?.RequiresWorkspace ?? toolInstance.RequiresWorkspace;
                }
                else if (metadataAttribute != null)
                {
                    toolName = ExtractToolNameFromType(toolType);
                    category = metadataAttribute.Category ?? ExtractCategoryFromNamespace(toolType);
                    
                    tags = (metadataAttribute.Tags?.Length > 0)
                        ? metadataAttribute.Tags.ToList()
                        : new List<string>();
                    
                    requiredServices = (metadataAttribute.RequiredServices?.Length > 0)
                        ? metadataAttribute.RequiredServices.ToList()
                        : new List<string>();
                    priority = metadataAttribute.Priority;
                    requiresWorkspace = metadataAttribute.RequiresWorkspace;
                }
                else
                {
                    return null;
                }

                var toolInfo = new ToolTypeInfo
                {
                    ToolType = toolType,
                    Name = toolName,
                    Category = category,
                    Tags = tags,
                    RequiredServices = requiredServices,
                    Priority = priority,
                    RequiresWorkspace = requiresWorkspace
                };

                return toolInfo;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractToolNameFromType(Type toolType)
        {
            var typeName = toolType.Name;
            if (typeName.EndsWith("Tool"))
            {
                typeName = typeName.Substring(0, typeName.Length - 4);
            }
            
            return ConvertToSnakeCase(typeName);
        }

        private string ExtractCategoryFromNamespace(Type toolType)
        {
            var namespaceParts = toolType.Namespace?.Split('.') ?? new string[0];
            if (namespaceParts.Length >= 3)
            {
                return namespaceParts[2];
            }
            return "General";
        }

        private string ConvertToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return string.Empty;

            var result = new System.Text.StringBuilder();
            for (int i = 0; i < pascalCase.Length; i++)
            {
                if (i > 0 && char.IsUpper(pascalCase[i]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(pascalCase[i]));
            }
            return result.ToString();
        }

        private ITool? CreateToolInstanceForDiscovery(Type toolType)
        {
            try
            {
                var constructors = toolType.GetConstructors()
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
                            canCreate = false;
                            break;
                        }
                    }

                    if (canCreate)
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(toolType, parameterValues);
                            return instance as ITool;
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

        private List<string> CombineTagsFromSources(string[]? metadataTags, List<string>? instanceTags)
        {
            var combinedTags = new HashSet<string>();
            
            if (metadataTags != null && metadataTags.Length > 0)
            {
                foreach (var tag in metadataTags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        combinedTags.Add(tag);
                    }
                }
            }
            
            if (instanceTags != null && instanceTags.Count > 0)
            {
                foreach (var tag in instanceTags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        combinedTags.Add(tag);
                    }
                }
            }
            
            return combinedTags.ToList();
        }
    }
}
