using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Core.Services
{
    public class ToolRegistry : IToolRegistry
    {
        private readonly ConcurrentDictionary<string, ToolRegistrationEntry> _tools = new ConcurrentDictionary<string, ToolRegistrationEntry>(StringComparer.OrdinalIgnoreCase);

        public void RegisterTool(ToolTypeInfo toolInfo)
        {
            if (toolInfo == null) throw new ArgumentNullException(nameof(toolInfo));
            
            var entry = new ToolRegistrationEntry
            {
                ToolInfo = toolInfo,
                Instance = null
            };
            
            var key = string.IsNullOrEmpty(toolInfo.Name) ? Guid.NewGuid().ToString("N") : toolInfo.Name;
            _tools[key] = entry;
        }

        public void RegisterTool(ITool toolInstance)
        {
            if (toolInstance == null) throw new ArgumentNullException(nameof(toolInstance));
            
            var registeredInfo = toolInstance.GetToolInfo();
            var toolType = toolInstance.GetType();
            
            ToolTypeInfo toolInfo;
            
            if (_tools.TryGetValue(registeredInfo.Name!, out var existingEntry) && existingEntry.ToolInfo != null)
            {
                toolInfo = existingEntry.ToolInfo;
            }
            else
            {
                toolInfo = new ToolTypeInfo
                {
                    ToolType = toolType,
                    Name = registeredInfo.Name,
                    Category = toolInstance.Category,
                    Tags = new List<string>(toolInstance.Tags),
                    RequiredServices = new List<string>(toolInstance.RequiredServices),
                    Priority = toolInstance.Priority,
                    RequiresWorkspace = toolInstance.RequiresWorkspace
                };
            }
            
            var entry = new ToolRegistrationEntry
            {
                ToolInfo = toolInfo,
                Instance = toolInstance
            };
            
            var key = string.IsNullOrEmpty(toolInfo.Name) ? Guid.NewGuid().ToString("N") : toolInfo.Name;
            _tools[key] = entry;
        }

        public ITool? GetTool(string name)
        {
            if (string.IsNullOrEmpty(name)) 
                return null;

            if (_tools.TryGetValue(name!, out var entry))
            {
                return entry.Instance;
            }
            return null;
        }

        public ToolTypeInfo? GetToolInfo(string name)
        {
            if (string.IsNullOrEmpty(name)) 
                return null;

            if (_tools.TryGetValue(name!, out var entry))
            {
                return entry.ToolInfo;
            }
            return null;
        }

        public List<ToolTypeInfo> GetAllTools()
        {
            return _tools.Values.Select(e => e.ToolInfo).ToList();
        }

        public List<ToolTypeInfo> GetToolsByCategory(string category)
        {
            return _tools.Values
                .Where(e => string.Equals(e.ToolInfo.Category, category, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.ToolInfo)
                .ToList();
        }

        public List<ToolTypeInfo> GetToolsByTag(string tag)
        {
            return _tools.Values
                .Where(e => e.ToolInfo.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                .Select(e => e.ToolInfo)
                .ToList();
        }

        public List<ToolTypeInfo> QueryTools(Func<ToolTypeInfo, bool> predicate)
        {
            return _tools.Values
                .Where(e => predicate(e.ToolInfo))
                .Select(e => e.ToolInfo)
                .ToList();
        }

        public bool IsToolRegistered(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return _tools.ContainsKey(name!);
        }
    }
}
