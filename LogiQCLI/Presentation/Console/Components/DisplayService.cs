using System;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Models.Modes.Interfaces;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class DisplayService : IDisplayService
    {
        private readonly ApplicationSettings _settings;
        private readonly IModeManager _modeManager;
        private readonly HeaderRenderer _headerRenderer;
        private readonly MessageRenderer _messageRenderer;

        public DisplayService(ApplicationSettings settings, IModeManager modeManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
            _headerRenderer = new HeaderRenderer(_settings, _modeManager);
            _messageRenderer = new MessageRenderer(_settings.DefaultModel ?? "ASSISTANT");
        }

        public void InitializeDisplay()
        {
            AnsiConsole.Clear();
            _headerRenderer.RenderHeader();
            _headerRenderer.RenderWelcomeMessage();
            _messageRenderer.RenderChatArea();
        }

        public Action GetInitializeDisplayAction()
        {
            return InitializeDisplay;
        }
    }
} 