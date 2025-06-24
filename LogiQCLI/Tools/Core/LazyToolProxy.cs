using System;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Core.Services
{
    public class LazyToolProxy : ITool
    {
        private readonly ToolTypeInfo _toolInfo;
        private readonly IToolFactory _toolFactory;
        private ITool? _actualTool;
        private readonly object _lock = new object();

        public LazyToolProxy(ToolTypeInfo toolInfo, IToolFactory toolFactory)
        {
            _toolInfo = toolInfo ?? throw new ArgumentNullException(nameof(toolInfo));
            _toolFactory = toolFactory ?? throw new ArgumentNullException(nameof(toolFactory));
        }

        public override RegisteredTool GetToolInfo()
        {
            if (_actualTool != null)
            {
                return _actualTool.GetToolInfo();
            }

            return new RegisteredTool
            {
                Name = _toolInfo.Name,
                Description = $"Tool '{_toolInfo.Name}' (lazy-loaded)",
                Parameters = new Tools.Core.Objects.Parameters()
            };
        }

        public override async Task<string> Execute(string args)
        {
            var tool = GetOrCreateTool();
            return await tool.Execute(args);
        }

        public override string Category => _toolInfo.Category;

        public override List<string> Tags => _toolInfo.Tags;

        public override List<string> RequiredServices => _toolInfo.RequiredServices;

        public override int Priority => _toolInfo.Priority;

        public override bool RequiresWorkspace => _toolInfo.RequiresWorkspace;

        private ITool GetOrCreateTool()
        {
            if (_actualTool != null)
            {
                return _actualTool;
            }

            lock (_lock)
            {
                if (_actualTool != null)
                {
                    return _actualTool;
                }

                _actualTool = _toolFactory.CreateTool(_toolInfo);
                return _actualTool;
            }
        }
    }
}