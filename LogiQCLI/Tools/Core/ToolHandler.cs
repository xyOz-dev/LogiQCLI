using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Tools.Core
{
    public class ToolHandler
    {
        private readonly Dictionary<string, ITool> _tools = new Dictionary<string, ITool>();
        private readonly IModeManager _modeManager;
        private readonly IToolRegistry? _toolRegistry;
        private readonly IToolFactory? _toolFactory;

        public ToolHandler(IModeManager modeManager)
        {
            _modeManager = modeManager;
        }

        public ToolHandler(IToolRegistry toolRegistry, IModeManager modeManager, IToolFactory toolFactory)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
            _toolFactory = toolFactory ?? throw new ArgumentNullException(nameof(toolFactory));
        }

        public void RegisterTool(ITool tool)
        {
            _tools[tool.GetToolInfo().Name] = tool;
            
            if (_toolRegistry != null)
            {
                _toolRegistry.RegisterTool(tool);
            }
        }

        public async Task<string> ExecuteTool(string name, string args)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Error: Tool name is required";
            }
            
            ITool? tool = null;
            
            if (_toolRegistry != null)
            {
                tool = _toolRegistry.GetTool(name);
                
                if (tool == null && _toolFactory != null)
                {
                    var toolInfo = _toolRegistry.GetToolInfo(name);
                    if (toolInfo != null && _toolFactory.CanCreateTool(toolInfo))
                    {
                        tool = _toolFactory.CreateTool(toolInfo);
                    }
                }
            }
            
            if (tool == null && _tools.TryGetValue(name, out var legacyTool))
            {
                tool = legacyTool;
            }
            
            if (tool == null)
            {
                var availableTools = GetAllowedToolNames();
                return $"Error: Tool '{name}' not found. Available tools: {string.Join(", ", availableTools)}";
            }

            if (!IsToolAllowedInCurrentMode(name, tool))
            {
                var currentMode = _modeManager.GetCurrentMode();
                return $"Error: Tool '{name}' is not allowed in current mode '{currentMode.Name}'. Available tools: {string.Join(", ", GetAllowedToolNames())}";
            }
            
            try
            {
                return await tool.Execute(args);
            }
            catch (Exception ex)
            {
                return $"Error executing tool '{name}': {ex.Message}";
            }
        }

        public Tool[] GetToolDefinitions()
        {
            var tools = new List<ITool>();
            
            if (_toolRegistry != null)
            {
                var allToolInfos = _toolRegistry.GetAllTools();
                foreach (var toolInfo in allToolInfos)
                {
                    if (IsToolAllowedInCurrentMode(toolInfo.Name, toolInfo.Category, toolInfo.Tags))
                    {
                        var tool = _toolRegistry.GetTool(toolInfo.Name);
                        if (tool != null)
                        {
                            tools.Add(tool);
                        }
                        else if (_toolFactory != null && _toolFactory.CanCreateTool(toolInfo))
                        {
                            try
                            {
                                tool = _toolFactory.CreateTool(toolInfo);
                                tools.Add(tool);
                            }
                            catch { }
                        }
                    }
                }
            }
            
            foreach (var kvp in _tools)
            {
                if (IsToolAllowedInCurrentMode(kvp.Key, kvp.Value) && !tools.Any(t => t.GetToolInfo().Name == kvp.Key))
                {
                    tools.Add(kvp.Value);
                }
            }
            
            return tools.Select(t =>
            {
                var info = t.GetToolInfo();
                return new Tool
                {
                    Type = "function",
                    Function = new Function
                    {
                        Name = info.Name,
                        Description = info.Description,
                        Parameters = info.Parameters
                    }
                };
            }).ToArray();
        }

        private bool IsToolAllowedInCurrentMode(string toolName, ITool tool)
        {
            return IsToolAllowedInCurrentMode(toolName, tool.Category, tool.Tags);
        }

        private bool IsToolAllowedInCurrentMode(string toolName, string? category = null, List<string>? tags = null)
        {
            var currentMode = _modeManager.GetCurrentMode();
            
            return currentMode.IsToolAllowed(toolName, category, tags);
        }

        private List<string> GetAllowedToolNames()
        {
            var allowedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentMode = _modeManager.GetCurrentMode();
            
            if (_toolRegistry != null)
            {
                var allTools = _toolRegistry.GetAllTools();
                foreach (var toolInfo in allTools)
                {
                    if (currentMode.IsToolAllowed(toolInfo.Name, toolInfo.Category, toolInfo.Tags))
                    {
                        allowedNames.Add(toolInfo.Name);
                    }
                }
            }
            
            foreach (var kvp in _tools)
            {
                if (currentMode.IsToolAllowed(kvp.Key, kvp.Value.Category, kvp.Value.Tags))
                {
                    allowedNames.Add(kvp.Key);
                }
            }
            
            return allowedNames.ToList();
        }
    }
}
