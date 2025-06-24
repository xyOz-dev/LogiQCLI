using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogiQCLI.Core.Models.Configuration;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class InteractiveConfigurationManager
    {
        private readonly ApplicationSettings _settings;

        public InteractiveConfigurationManager()
        {
            _settings = new ApplicationSettings();
        }

        public ApplicationSettings ConfigureInteractively()
        {
            RenderConfigurationHeader();
            ConfigureWorkspace();
            ConfigureModel();
            ConfigureApiKey();
            DisplaySelectedConfiguration();
            return _settings;
        }

        private void RenderConfigurationHeader()
        {
            var panel = new Panel("[bold cyan]Interactive Configuration[/]")
                .Header("[cyan]Setup[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(1, 0);
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private void ConfigureWorkspace()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var defaultWorkspace = currentDirectory;

            AnsiConsole.MarkupLine($"[cyan]Workspace Directory[/]");
            AnsiConsole.MarkupLine($"[dim]Current: {currentDirectory}[/]");
            AnsiConsole.WriteLine();

            var workspace = AnsiConsole.Ask<string>(
                $"[green]Enter workspace path[/] [dim](press Enter for current directory)[/]:",
                defaultWorkspace);

            if (!Directory.Exists(workspace))
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Directory '{workspace}' does not exist. Creating it...[/]");
                try
                {
                    Directory.CreateDirectory(workspace);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error creating directory: {ex.Message}[/]");
                    AnsiConsole.MarkupLine($"[yellow]Using current directory instead: {currentDirectory}[/]");
                    workspace = currentDirectory;
                }
            }

            _settings.Workspace = workspace;
            AnsiConsole.WriteLine();
        }

        private void ConfigureModel()
        {
            var defaultModel = "google/gemini-2.5-pro";
            
            AnsiConsole.MarkupLine("[cyan]AI Model Selection[/]");
            AnsiConsole.MarkupLine($"[dim]Available models:[/]");
            
            for (int i = 0; i < _settings.AvailableModels.Count; i++)
            {
                var model = _settings.AvailableModels[i];
                var prefix = model == defaultModel ? "[green]✓[/]" : " ";
                AnsiConsole.MarkupLine($"  {prefix} [yellow]{i + 1}[/]. {model}");
            }
            AnsiConsole.WriteLine();

            var modelChoice = AnsiConsole.Ask<string>(
                $"[green]Select model by number or enter custom model name[/] [dim](press Enter for default: {defaultModel})[/]:",
                defaultModel);

            if (int.TryParse(modelChoice, out int index) && index >= 1 && index <= _settings.AvailableModels.Count)
            {
                _settings.DefaultModel = _settings.AvailableModels[index - 1];
            }
            else if (!string.IsNullOrWhiteSpace(modelChoice))
            {
                _settings.DefaultModel = modelChoice;
            }
            else
            {
                _settings.DefaultModel = defaultModel;
            }

            AnsiConsole.WriteLine();
        }

       private void ConfigureApiKey()
       {
           AnsiConsole.MarkupLine("[cyan]API Key Configuration[/]");
           AnsiConsole.MarkupLine("[dim]You can add multiple API keys and switch between them.[/]");
           AnsiConsole.WriteLine();

           var addMoreKeys = true;
           while (addMoreKeys)
           {
               var nickname = AnsiConsole.Ask<string>("[green]Enter a nickname for the API key:[/] ");
               var apiKey = AnsiConsole.Prompt(
                   new TextPrompt<string>($"[green]Enter API key for '{nickname}':[/] ")
                       .PromptStyle("green")
                       .Secret());

               _settings.ApiKeys.Add(new ApiKeySettings { Nickname = nickname, ApiKey = apiKey });
               
               if (_settings.ApiKeys.Count == 1)
               {
                   _settings.ActiveApiKeyNickname = nickname;
               }

               addMoreKeys = AnsiConsole.Confirm("[green]Add another API key?[/]", false);
               AnsiConsole.WriteLine();
           }
       }

        private void DisplaySelectedConfiguration()
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green)
                .AddColumn("[bold]Setting[/]")
                .AddColumn("[bold]Value[/]");

            table.AddRow("[cyan]Workspace[/]", $"[green]{_settings.Workspace}[/]");
            table.AddRow("[cyan]Model[/]", $"[green]{_settings.DefaultModel}[/]");
            var activeKey = _settings.GetActiveApiKey();
            if (activeKey != null)
            {
                table.AddRow("[cyan]Active API Key[/]", $"[green]{activeKey.Nickname} ({activeKey.GetObfuscatedKey()})[/]");
            }
            else
            {
                table.AddRow("[cyan]Active API Key[/]", "[yellow]Not Set[/]");
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var confirm = AnsiConsole.Confirm("[green]Continue with these settings?[/]", true);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Configuration cancelled. Exiting...[/]");
                Environment.Exit(0);
            }
        }
    }
}
