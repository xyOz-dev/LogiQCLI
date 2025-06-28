using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Presentation.Console.Components.Objects;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Core.Models.Modes;
using LogiQCLI.Tools.Core.Interfaces;
using Spectre.Console;

namespace LogiQCLI.Commands.Mode
{
    [CommandMetadata("Mode", Tags = new[] { "mode", "essential" })]
    public class ModeCommand : ICommand
    {
        private readonly IModeManager _modeManager;
        private readonly ChatSession _chatSession;
        private readonly Action _initializeDisplay;
        private readonly IToolRegistry _toolRegistry;

        public ModeCommand(IModeManager modeManager, ChatSession chatSession, Action initializeDisplay, IToolRegistry toolRegistry)
        {
            _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "mode",
                Description = "Mode management. Usage: /mode [list|current|switch <mode-id>|info <mode-id>|create]",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        action = new
                        {
                            type = "string",
                            description = "Action to perform: list, current, switch, info, create"
                        },
                        modeId = new
                        {
                            type = "string",
                            description = "Mode ID for switch/info actions"
                        }
                    }
                }
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    return Task.FromResult(ShowCurrentMode());
                }

                var parts = args.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var subCommand = parts[0].ToLowerInvariant();
                var subArgs = parts.Length > 1 ? parts[1] : string.Empty;

                switch (subCommand)
                {
                    case "list":
                        return Task.FromResult(ShowAvailableModes());
                    case "current":
                        return Task.FromResult(ShowCurrentMode());
                    case "switch":
                        return Task.FromResult(HandleModeSwitch(subArgs));
                    case "info":
                        return Task.FromResult(ShowModeInfo(subArgs));
                    case "create":
                        return Task.FromResult(HandleModeCreate());
                    default:
                        return Task.FromResult("[red]Unknown mode subcommand. Use: list, current, switch <mode-id>, info <mode-id>, create[/]");
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error executing mode command: {ex.Message}[/]");
            }
        }

        private string ShowCurrentMode()
        {
            var currentMode = _modeManager.GetCurrentMode();
            
            var modeData = new Dictionary<string, string>
            {
                { "ID", currentMode.Id },
                { "Name", currentMode.Name },
                { "Description", currentMode.Description },
                { "Type", currentMode.IsBuiltIn ? "Built-in" : "Custom" },
                { "Allowed Tools", currentMode.AllowedTools.Count.ToString() }
            };

            TableFormatter.RenderKeyValueTable("Current Mode", modeData, Color.Green);
            return "";
        }

        private string ShowAvailableModes()
        {
            var modes = _modeManager.GetAvailableModes();
            var currentMode = _modeManager.GetCurrentMode();
            
            var modeRows = modes.Select(m => new ModeTableRow
            {
                Id = m.Id,
                Name = m.Name,
                IsBuiltIn = m.IsBuiltIn,
                ToolCount = m.AllowedTools.Count
            });

            TableFormatter.RenderModesTable(modeRows, currentMode.Id);
            return "";
        }

        private string HandleModeSwitch(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                return "[red]Mode ID is required. Usage: /mode switch <mode-id>[/]";
            }

            var targetMode = _modeManager.GetMode(modeId);
            if (targetMode == null)
            {
                return $"[red]Mode '{modeId}' not found. Use '/mode list' to see available modes.[/]";
            }

            var currentMode = _modeManager.GetCurrentMode();
            if (currentMode.Id == modeId)
            {
                return $"[yellow]Already in mode '{targetMode.Name}'.[/]";
            }

            if (_modeManager.SetCurrentMode(modeId))
            {
                _chatSession.UpdateSystemPrompt();
                _initializeDisplay();
                return $"[green]Successfully switched to mode '{targetMode.Name}'.[/]";
            }
            else
            {
                return $"[red]Failed to switch to mode '{modeId}'.[/]";
            }
        }

        private string ShowModeInfo(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                return "[red]Mode ID is required. Usage: /mode info <mode-id>[/]";
            }

            var mode = _modeManager.GetMode(modeId);
            if (mode == null)
            {
                return $"[red]Mode '{modeId}' not found. Use '/mode list' to see available modes.[/]";
            }

            var toolsList = mode.AllowedTools.Count > 0
                ? string.Join(", ", mode.AllowedTools.Take(5)) + (mode.AllowedTools.Count > 5 ? $" (+{mode.AllowedTools.Count - 5} more)" : "")
                : "None";

            var output = new StringBuilder();
            output.AppendLine($"[bold cyan]{mode.Name}[/]");
            output.AppendLine($"[dim]ID:[/] [white]{mode.Id}[/]");
            output.AppendLine($"[dim]Type:[/] [cyan]{(mode.IsBuiltIn ? "Built-in" : "Custom")}[/]");
            output.AppendLine($"[dim]Description:[/]");
            output.AppendLine($"[white]{mode.Description}[/]");
            output.AppendLine($"[dim]Allowed Tools ({mode.AllowedTools.Count}):[/]");
            output.AppendLine($"[yellow]{toolsList}[/]");
            
            if (!string.IsNullOrEmpty(mode.PreferredModel))
            {
                output.AppendLine($"[dim]Preferred Model:[/] [cyan]{mode.PreferredModel}[/]");
            }
            else
            {
                output.AppendLine("[dim]Preferred Model:[/] [grey]Default[/]");
            }

            return output.ToString();
        }

        private string HandleModeCreate()
        {
            try
            {
                AnsiConsole.Clear();
                RenderCreateModeHeader();

                var modeBuilder = new ModeBuilder();

                // Configure basic properties
                ConfigureModeBasics(modeBuilder);
                
                // Configure system prompt
                ConfigureSystemPrompt(modeBuilder);
                
                // Configure preferred model
                ConfigurePreferredModel(modeBuilder);
                
                // Configure tool permissions
                ConfigureToolPermissions(modeBuilder);

                // Review and confirm
                var mode = modeBuilder.Build();
                if (ReviewAndConfirmMode(mode))
                {
                    _modeManager.AddCustomMode(mode);
                    AnsiConsole.MarkupLine($"[green]✓ Custom mode '{mode.Name}' created successfully![/]");
                    
                    var switchToNew = AnsiConsole.Confirm($"[green]Switch to the new mode '{mode.Name}' now?[/]", true);
                    if (switchToNew)
                    {
                        _modeManager.SetCurrentMode(mode.Id);
                        _chatSession.UpdateSystemPrompt();
                        _initializeDisplay();
                        return $"[green]Successfully created and switched to mode '{mode.Name}'.[/]";
                    }
                    
                    return $"[green]Mode '{mode.Name}' created successfully. Use '/mode switch {mode.Id}' to activate it.[/]";
                }
                else
                {
                    return "[yellow]Mode creation cancelled.[/]";
                }
            }
            catch (Exception ex)
            {
                return $"[red]Error creating mode: {ex.Message}[/]";
            }
        }

        private void RenderCreateModeHeader()
        {
            var panel = new Panel("[bold cyan]Create Custom Mode[/]")
                .Header("[cyan]Mode Creation Wizard[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(1, 0);
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            AnsiConsole.MarkupLine("[dim]Create a custom mode with specific tools, model preferences, and system prompts.[/]");
            AnsiConsole.WriteLine();
        }

        private void ConfigureModeBasics(ModeBuilder modeBuilder)
        {
            AnsiConsole.MarkupLine("[cyan]Basic Mode Configuration[/]");
            AnsiConsole.WriteLine();

            var modeId = AnsiConsole.Ask<string>("[green]Enter mode ID[/] [dim](lowercase, no spaces, unique identifier)[/]: ");
            while (string.IsNullOrWhiteSpace(modeId) || _modeManager.GetMode(modeId) != null)
            {
                if (string.IsNullOrWhiteSpace(modeId))
                {
                    AnsiConsole.MarkupLine("[red]Mode ID cannot be empty.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Mode ID '{modeId}' already exists.[/]");
                }
                modeId = AnsiConsole.Ask<string>("[green]Enter a different mode ID:[/] ");
            }
            modeBuilder.WithId(modeId);

            var modeName = AnsiConsole.Ask<string>("[green]Enter mode name[/] [dim](display name)[/]: ");
            while (string.IsNullOrWhiteSpace(modeName))
            {
                AnsiConsole.MarkupLine("[red]Mode name cannot be empty.[/]");
                modeName = AnsiConsole.Ask<string>("[green]Enter mode name:[/] ");
            }
            modeBuilder.WithName(modeName);

            var description = AnsiConsole.Ask<string>("[green]Enter mode description[/] [dim](what this mode is designed for)[/]: ");
            while (string.IsNullOrWhiteSpace(description))
            {
                AnsiConsole.MarkupLine("[red]Description cannot be empty.[/]");
                description = AnsiConsole.Ask<string>("[green]Enter mode description:[/] ");
            }
            modeBuilder.WithDescription(description);

            AnsiConsole.WriteLine();
        }

        private void ConfigureSystemPrompt(ModeBuilder modeBuilder)
        {
            AnsiConsole.MarkupLine("[cyan]System Prompt Configuration[/]");
            AnsiConsole.MarkupLine("[dim]The system prompt defines the AI's behavior and capabilities in this mode.[/]");
            AnsiConsole.MarkupLine("[dim]You can use multiple lines, paste content, and include special instructions.[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[green]Enter system prompt (end with an empty line):[/]");
            var promptLines = new List<string>();
            string? line;
            while (!string.IsNullOrEmpty(line = Console.ReadLine()))
            {
                promptLines.Add(line);
            }
            
            var systemPrompt = string.Join(Environment.NewLine, promptLines);
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = "You are a helpful AI assistant.";
                AnsiConsole.MarkupLine("[yellow]Using default system prompt: 'You are a helpful AI assistant.'[/]");
            }
            
            modeBuilder.WithSystemPrompt(systemPrompt);
            AnsiConsole.WriteLine();
        }

        private void ConfigurePreferredModel(ModeBuilder modeBuilder)
        {
            AnsiConsole.MarkupLine("[cyan]Preferred Model Configuration[/]");
            AnsiConsole.WriteLine();

            var setPreferredModel = AnsiConsole.Confirm("[green]Set a preferred model for this mode?[/]", false);
            
            if (setPreferredModel)
            {
                var currentModel = _chatSession.Model;
                var useCurrentModel = AnsiConsole.Confirm($"[green]Use current model ({currentModel}) as preferred?[/]", true);
                
                if (useCurrentModel)
                {
                    modeBuilder.WithPreferredModel(currentModel);
                    AnsiConsole.MarkupLine($"[green]✓ Preferred model set to: {currentModel}[/]");
                }
                else
                {
                    var customModel = AnsiConsole.Ask<string>("[green]Enter preferred model name:[/] ");
                    if (!string.IsNullOrWhiteSpace(customModel))
                    {
                        modeBuilder.WithPreferredModel(customModel);
                        AnsiConsole.MarkupLine($"[green]✓ Preferred model set to: {customModel}[/]");
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No preferred model set - will use default.[/]");
            }
            
            AnsiConsole.WriteLine();
        }

        private void ConfigureToolPermissions(ModeBuilder modeBuilder)
        {
            AnsiConsole.MarkupLine("[cyan]Tool Permissions Configuration[/]");
            AnsiConsole.MarkupLine("[dim]Configure which tools are available in this mode.[/]");
            AnsiConsole.WriteLine();

            var configurationOptions = new[]
            {
                "Allow all tools",
                "Allow specific categories",
                "Allow specific tools",
                "Allow categories with exclusions",
                "Custom configuration"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]How would you like to configure tool permissions?[/]")
                    .AddChoices(configurationOptions));

            switch (choice)
            {
                case "Allow all tools":
                    modeBuilder.AllowAllTools();
                    AnsiConsole.MarkupLine("[green]✓ All tools will be available in this mode.[/]");
                    break;

                case "Allow specific categories":
                    ConfigureAllowedCategories(modeBuilder);
                    break;

                case "Allow specific tools":
                    ConfigureSpecificTools(modeBuilder);
                    break;

                case "Allow categories with exclusions":
                    ConfigureCategoriesWithExclusions(modeBuilder);
                    break;

                case "Custom configuration":
                    ConfigureCustomToolPermissions(modeBuilder);
                    break;
            }

            AnsiConsole.WriteLine();
        }

        private void ConfigureAllowedCategories(ModeBuilder modeBuilder)
        {
            var availableCategories = GetAvailableToolCategories();
            
            AnsiConsole.MarkupLine("[yellow]Available tool categories:[/]");
            foreach (var category in availableCategories)
            {
                AnsiConsole.MarkupLine($"  • [cyan]{category}[/]");
            }
            AnsiConsole.WriteLine();

            var selectedCategories = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[green]Select categories to allow:[/]")
                    .Required()
                    .AddChoices(availableCategories));

            foreach (var category in selectedCategories)
            {
                modeBuilder.AllowCategory(category);
            }

            AnsiConsole.MarkupLine($"[green]✓ Allowed categories: {string.Join(", ", selectedCategories)}[/]");
        }

        private void ConfigureSpecificTools(ModeBuilder modeBuilder)
        {
            var availableTools = GetAvailableTools();
            
            if (availableTools.Count > 20)
            {
                AnsiConsole.MarkupLine($"[yellow]There are {availableTools.Count} available tools. This might be a long list.[/]");
                var proceed = AnsiConsole.Confirm("[green]Continue with tool selection?[/]", true);
                if (!proceed)
                {
                    AnsiConsole.MarkupLine("[yellow]Falling back to category-based selection.[/]");
                    ConfigureAllowedCategories(modeBuilder);
                    return;
                }
            }

            var selectedTools = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[green]Select tools to allow:[/]")
                    .Required()
                    .PageSize(15)
                    .MoreChoicesText("[grey](Move up and down to reveal more tools)[/]")
                    .AddChoices(availableTools));

            foreach (var tool in selectedTools)
            {
                modeBuilder.AllowTool(tool);
            }

            AnsiConsole.MarkupLine($"[green]✓ Allowed {selectedTools.Count} specific tools.[/]");
        }

        private void ConfigureCategoriesWithExclusions(ModeBuilder modeBuilder)
        {
            ConfigureAllowedCategories(modeBuilder);
            
            AnsiConsole.WriteLine();
            var addExclusions = AnsiConsole.Confirm("[green]Add any category exclusions?[/]", false);
            
            if (addExclusions)
            {
                var availableCategories = GetAvailableToolCategories();
                var excludeCategories = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("[green]Select categories to exclude:[/]")
                        .AddChoices(availableCategories));

                foreach (var category in excludeCategories)
                {
                    modeBuilder.ExcludeCategory(category);
                }

                if (excludeCategories.Any())
                {
                    AnsiConsole.MarkupLine($"[green]✓ Excluded categories: {string.Join(", ", excludeCategories)}[/]");
                }
            }
        }

        private void ConfigureCustomToolPermissions(ModeBuilder modeBuilder)
        {
            var keepConfiguring = true;
            
            while (keepConfiguring)
            {
                var options = new[]
                {
                    "Allow tool category",
                    "Exclude tool category", 
                    "Allow specific tool",
                    "Allow by tag",
                    "Exclude by tag",
                    "Finish configuration"
                };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]What would you like to configure?[/]")
                        .AddChoices(options));

                switch (choice)
                {
                    case "Allow tool category":
                        var allowCategory = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[green]Select category to allow:[/]")
                                .AddChoices(GetAvailableToolCategories()));
                        modeBuilder.AllowCategory(allowCategory);
                        AnsiConsole.MarkupLine($"[green]✓ Allowed category: {allowCategory}[/]");
                        break;

                    case "Exclude tool category":
                        var excludeCategory = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[green]Select category to exclude:[/]")
                                .AddChoices(GetAvailableToolCategories()));
                        modeBuilder.ExcludeCategory(excludeCategory);
                        AnsiConsole.MarkupLine($"[green]✓ Excluded category: {excludeCategory}[/]");
                        break;

                    case "Allow specific tool":
                        var allowTool = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[green]Select tool to allow:[/]")
                                .PageSize(15)
                                .MoreChoicesText("[grey](Move up and down to reveal more tools)[/]")
                                .AddChoices(GetAvailableTools()));
                        modeBuilder.AllowTool(allowTool);
                        AnsiConsole.MarkupLine($"[green]✓ Allowed tool: {allowTool}[/]");
                        break;

                    case "Allow by tag":
                        var allowTag = AnsiConsole.Ask<string>("[green]Enter tag to allow:[/] ");
                        if (!string.IsNullOrWhiteSpace(allowTag))
                        {
                            modeBuilder.AllowTag(allowTag);
                            AnsiConsole.MarkupLine($"[green]✓ Allowed tag: {allowTag}[/]");
                        }
                        break;

                    case "Exclude by tag":
                        var excludeTag = AnsiConsole.Ask<string>("[green]Enter tag to exclude:[/] ");
                        if (!string.IsNullOrWhiteSpace(excludeTag))
                        {
                            modeBuilder.ExcludeTag(excludeTag);
                            AnsiConsole.MarkupLine($"[green]✓ Excluded tag: {excludeTag}[/]");
                        }
                        break;

                    case "Finish configuration":
                        keepConfiguring = false;
                        break;
                }
                
                if (keepConfiguring)
                {
                    AnsiConsole.WriteLine();
                }
            }
        }

        private bool ReviewAndConfirmMode(LogiQCLI.Core.Models.Modes.Mode mode)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan]Review New Mode Configuration[/]");
            AnsiConsole.WriteLine();

            var reviewData = new Dictionary<string, string>
            {
                { "ID", mode.Id },
                { "Name", mode.Name },
                { "Description", mode.Description },
                { "Preferred Model", mode.PreferredModel ?? "[dim]Default[/]" },
                { "Tool Categories", mode.AllowedCategories.Any() ? string.Join(", ", mode.AllowedCategories) : "[dim]None specified[/]" },
                { "Excluded Categories", mode.ExcludedCategories.Any() ? string.Join(", ", mode.ExcludedCategories) : "[dim]None[/]" },
                { "Allowed Tags", mode.AllowedTags.Any() ? string.Join(", ", mode.AllowedTags) : "[dim]None specified[/]" },
                { "Excluded Tags", mode.ExcludedTags.Any() ? string.Join(", ", mode.ExcludedTags) : "[dim]None[/]" },
                { "Specific Tools", mode.AllowedTools.Any() ? $"{mode.AllowedTools.Count} tools" : "[dim]None specified[/]" }
            };

            TableFormatter.RenderKeyValueTable("New Mode Configuration", reviewData, Color.Blue);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]System Prompt Preview:[/]");
            var promptPreview = mode.SystemPrompt.Length > 200 
                ? mode.SystemPrompt.Substring(0, 200) + "..."
                : mode.SystemPrompt;
            
            var promptPanel = new Panel(Markup.Escape(promptPreview))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Padding(1, 0);
            
            AnsiConsole.Write(promptPanel);
            AnsiConsole.WriteLine();

            return AnsiConsole.Confirm("[green]Create this mode?[/]", true);
        }

        private List<string> GetAvailableToolCategories()
        {
            return _toolRegistry?.GetAllTools()
                .SelectMany(t => new[] { t.Category })
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList() ?? new List<string>();
        }

        private List<string> GetAvailableTools()
        {
            return _toolRegistry?.GetAllTools()
                .Select(t => t.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n)
                .ToList() ?? new List<string>();
        }
    }
} 
