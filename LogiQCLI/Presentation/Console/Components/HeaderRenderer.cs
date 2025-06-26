using System;
using LogiQCLI.Core.Models.Configuration;
using Spectre.Console;
using LogiQCLI.Core.Models.Modes.Interfaces;

namespace LogiQCLI.Presentation.Console.Components
{
    public class HeaderRenderer
    {
        private readonly ApplicationSettings _settings;
        private readonly IModeManager _modeManager;
        
        public HeaderRenderer(ApplicationSettings settings, IModeManager modeManager)
        {
            _settings = settings;
            _modeManager = modeManager;
        }

        public void RenderHeader()
        {
            RenderSystemInfo();
            AnsiConsole.WriteLine();
        }

        private void RenderSystemInfo()
        {
            AnsiConsole.WriteLine();
            
            var currentMode = _modeManager.GetCurrentMode();
            var modeDisplay = !string.IsNullOrEmpty(currentMode.Name) ? currentMode.Name : currentMode.Id;
            
            var infoPanel = new Panel(new Rows(
                new Markup($"[dim]Workspace:[/] [cyan]{_settings.Workspace ?? "Unknown"}[/]"),
                new Markup($"[dim]Model:[/] [cyan]{_settings.DefaultModel ?? "Unknown"}[/]"),
                new Markup($"[dim]Mode:[/] [green]{modeDisplay}[/]"),
                new Markup($"[dim]Time:[/] [cyan]{DateTime.Now:yyyy-MM-dd HH:mm:ss}[/]")
            ))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.FromHex("#444444"))
            .Padding(1, 0);

            AnsiConsole.Write(Align.Center(infoPanel));
        }

        public void RenderWelcomeMessage()
        {
            var welcomeMessages = new[]
            {
                "Ready to assist with your coding needs.",
                "Let's build something amazing together.",
                "Your code, enhanced by AI.",
                "Intelligent coding assistance at your service."
            };

            var random = new Random();
            var message = welcomeMessages[random.Next(welcomeMessages.Length)];

            AnsiConsole.Write(Align.Center(new Markup($"[dim]{message}[/]")));
            AnsiConsole.WriteLine();
            
            // Add helpful tip about the help command
            AnsiConsole.Write(Align.Center(new Markup("[dim]💡 Tip: Type [green]/help[/] to see all available commands[/]")));
            AnsiConsole.WriteLine();
        }
    }
}
