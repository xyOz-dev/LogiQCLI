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
            ConfigureProvider();
            ConfigureModel();
            ConfigureApiKey();
            ConfigureGitHub();
            ConfigureTavily();
            ConfigureExperimental();
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

        private void ConfigureProvider()
        {
            AnsiConsole.MarkupLine("[cyan]Provider Selection[/]");
            var providers = new[] { "openrouter", "requesty", "lmstudio" };
            var providerChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Choose default provider:[/]")
                    .AddChoices(providers));

            _settings.DefaultProvider = providerChoice;
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

               _settings.ApiKeys.Add(new ApiKeySettings { Nickname = nickname, ApiKey = apiKey, Provider = _settings.DefaultProvider });
               
               if (_settings.ApiKeys.Count == 1)
               {
                   _settings.ActiveApiKeyNickname = nickname;
               }

               addMoreKeys = AnsiConsole.Confirm("[green]Add another API key?[/]", false);
               AnsiConsole.WriteLine();
           }
       }

        private void ConfigureGitHub()
        {
            AnsiConsole.MarkupLine("[cyan]GitHub Integration Configuration (Optional)[/]");
            AnsiConsole.MarkupLine("[dim]GitHub integration enables repository operations and issue management.[/]");
            AnsiConsole.WriteLine();

            var configureGitHub = AnsiConsole.Confirm("[green]Do you want to configure GitHub integration?[/]", false);
            
            if (configureGitHub)
            {
                var token = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Enter GitHub Personal Access Token:[/] ")
                        .PromptStyle("green")
                        .Secret()
                        .AllowEmpty());

                string? defaultOwner = null;
                string? defaultRepo = null;

                if (!string.IsNullOrWhiteSpace(token))
                {
                    var configureDefaults = AnsiConsole.Confirm("[green]Do you want to set default owner/repository?[/]", false);
                    
                    if (configureDefaults)
                    {
                        defaultOwner = AnsiConsole.Ask<string>("[green]Enter default GitHub owner/organization (optional):[/] ", string.Empty);
                        if (!string.IsNullOrWhiteSpace(defaultOwner))
                        {
                            defaultRepo = AnsiConsole.Ask<string>("[green]Enter default repository name (optional):[/] ", string.Empty);
                        }
                    }

                    _settings.GitHub = new GitHubSettings 
                    { 
                        Token = token,
                        DefaultOwner = string.IsNullOrWhiteSpace(defaultOwner) ? null : defaultOwner,
                        DefaultRepo = string.IsNullOrWhiteSpace(defaultRepo) ? null : defaultRepo
                    };
                    AnsiConsole.MarkupLine("[green]✓ GitHub configured successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Skipping GitHub configuration (no token provided).[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]GitHub configuration skipped.[/]");
            }

            AnsiConsole.WriteLine();
        }

        private void ConfigureTavily()
        {
            AnsiConsole.MarkupLine("[cyan]Tavily Search Configuration (Optional)[/]");
            AnsiConsole.MarkupLine("[dim]Tavily provides AI-optimized web search capabilities.[/]");
            AnsiConsole.WriteLine();

            var configureTavily = AnsiConsole.Confirm("[green]Do you want to configure Tavily search?[/]", false);
            
            if (configureTavily)
            {
                var apiKey = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Enter Tavily API key:[/] ")
                        .PromptStyle("green")
                        .Secret()
                        .AllowEmpty());

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _settings.Tavily = new TavilySettings { ApiKey = apiKey };
                    AnsiConsole.MarkupLine("[green]✓ Tavily configured successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Skipping Tavily configuration.[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]Tavily configuration skipped.[/]");
            }

            AnsiConsole.WriteLine();
        }

        private void ConfigureExperimental()
        {
            AnsiConsole.MarkupLine("[cyan]Experimental Features[/]");
            AnsiConsole.WriteLine();

            var enableDedup = AnsiConsole.Confirm("[green]Enable file read deduplication (remove old file reads from context)?[/]", _settings.Experimental.DeduplicateFileReads);
            _settings.Experimental.DeduplicateFileReads = enableDedup;

            AnsiConsole.WriteLine();
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
            table.AddRow("[cyan]Provider[/]", $"[green]{_settings.DefaultProvider}[/]");

            var activeKey = _settings.GetActiveApiKey();
            if (activeKey != null)
            {
                table.AddRow("[cyan]Active API Key[/]", $"[green]{activeKey.Nickname} ({activeKey.GetObfuscatedKey()})[/]");
            }
            else
            {
                table.AddRow("[cyan]Active API Key[/]", "[yellow]Not Set[/]");
            }

            var githubStatus = !string.IsNullOrEmpty(_settings.GitHub?.Token) ? "Configured" : "Not Set";
            table.AddRow("[cyan]GitHub[/]", githubStatus == "Configured" ? "[green]Configured[/]" : "[yellow]Not Set[/]");

            var tavilyStatus = !string.IsNullOrEmpty(_settings.Tavily?.ApiKey) ? "Configured" : "Not Set";
            table.AddRow("[cyan]Tavily[/]", tavilyStatus == "Configured" ? "[green]Configured[/]" : "[yellow]Not Set[/]");

            var dedupStatus = _settings.Experimental.DeduplicateFileReads ? "Enabled" : "Disabled";
            table.AddRow("[cyan]File Read Deduplication[/]", dedupStatus == "Enabled" ? "[green]Enabled[/]" : "[yellow]Disabled[/]");

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
