
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Core.Models.Modes;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Presentation.Console.Components.Objects;
using Spectre.Console;
using System.Text.RegularExpressions;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Presentation.Console.Components
{
    public class CommandHandler
    {
        private readonly ChatSession _chatSession;
        private readonly InputHandler _inputHandler;
        private readonly Action _initializeDisplay;
        private readonly ApplicationSettings _settings;
        private readonly ConfigurationService _configService;
        private readonly IModeManager _modeManager;
        private readonly IToolRegistry _toolRegistry;
        private readonly Dictionary<string, Func<string, Task>> _commands;

        public CommandHandler(
            ChatSession chatSession,
            InputHandler inputHandler,
            Action initializeDisplay,
            ApplicationSettings settings,
            ConfigurationService configService,
            IModeManager modeManager,
            IToolRegistry toolRegistry)
        {
            _chatSession = chatSession;
            _inputHandler = inputHandler;
            _initializeDisplay = initializeDisplay;
            _settings = settings;
            _configService = configService;
            _modeManager = modeManager;
            _toolRegistry = toolRegistry;
            _commands = new Dictionary<string, Func<string, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                { "/clear", HandleClearCommand },
                { "/exit", HandleExitCommand },
                { "/quit", HandleExitCommand },
                { "/model", HandleModelCommand },
                { "/workspace", HandleWorkspaceCommand },
                { "/switchkey", HandleSwitchKeyCommand },
                { "/addkey", HandleAddKeyCommand },
                { "/settings", HandleSettingsCommand },
                { "/mode", HandleModeCommand },
                { "/test", HandleTestCommand },
                { "/test-tools", HandleTestToolsCommand },
                { "/backups", HandleBackupsCommand },
                { "/restore", HandleRestoreCommand },
                { "/backup-diff", HandleBackupDiffCommand },
                { "/backup-cleanup", HandleBackupCleanupCommand },
                { "/backup-status", HandleBackupStatusCommand }
            };
        }

        public bool IsCommand(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Trim().StartsWith("/");
        }

        public async Task ExecuteCommand(string userInput)
        {
            var parts = userInput.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0];
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            if (_commands.TryGetValue(commandName, out var commandAction))
            {
                await commandAction(args);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Unknown command: {commandName}[/]");
            }
        }

        private Task HandleClearCommand(string args)
        {
            _chatSession.ClearHistory();
            _initializeDisplay();
            AnsiConsole.MarkupLine("[bold green]Chat history has been cleared.[/]");
            return Task.CompletedTask;
        }

        private Task HandleExitCommand(string args)
        {
            if (_inputHandler.GetConfirmation("Are you sure you want to exit?"))
            {
                AnsiConsole.Clear();
                Environment.Exit(0);
            }
            
            return Task.CompletedTask;
        }

        private Task HandleModelCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                AnsiConsole.MarkupLine($"[yellow]Current model: {_chatSession.Model}[/]");
            }
            else
            {
                _chatSession.Model = args.Trim();
                AnsiConsole.MarkupLine($"[green]Model updated to: {_chatSession.Model}[/]");
            }
            return Task.CompletedTask;
        }

        private Task HandleWorkspaceCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                var currentWorkspace = System.IO.Directory.GetCurrentDirectory();
                AnsiConsole.MarkupLine($"[yellow]Current workspace: {currentWorkspace}[/]");
            }
            else
            {
                var newWorkspace = args.Trim();
                
                if (!System.IO.Directory.Exists(newWorkspace))
                {
                    AnsiConsole.MarkupLine($"[red]Directory does not exist: {newWorkspace}[/]");
                    return Task.CompletedTask;
                }
                
                try
                {
                    System.IO.Directory.SetCurrentDirectory(newWorkspace);
                    AnsiConsole.MarkupLine($"[green]Workspace changed to: {newWorkspace}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Failed to change workspace: {ex.Message}[/]");
                }
            }
            return Task.CompletedTask;
        }

        private Task HandleSwitchKeyCommand(string args)
        {
            var availableKeys = _settings.ApiKeys;
            if (availableKeys.Count <= 1)
            {
                AnsiConsole.MarkupLine("[yellow]No other API keys available to switch to.[/]");
                return Task.CompletedTask;
            }

            var selection = new SelectionPrompt<string>()
                .Title("[green]Select an API key to activate[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more keys)[/]");

            foreach (var key in availableKeys)
            {
                var label = $"{key.Nickname} ({key.GetObfuscatedKey()})";
                if (key.Nickname == _settings.ActiveApiKeyNickname)
                {
                    label += " [cyan](current)[/]";
                }
                selection.AddChoice(label);
            }

            var selectedKeyLabel = AnsiConsole.Prompt(selection);
            var selectedNickname = selectedKeyLabel.Split(' ')[0];

            if (selectedNickname != _settings.ActiveApiKeyNickname)
            {
                _settings.ActiveApiKeyNickname = selectedNickname;
                _configService.SaveSettings(_settings);
                _initializeDisplay();
                AnsiConsole.MarkupLine($"[green]API key switched to '{selectedNickname}'.[/]");
            }

            return Task.CompletedTask;
        }

        private Task HandleAddKeyCommand(string args)
        {
            AnsiConsole.MarkupLine("[cyan]Add New API Key[/]");
            var nickname = AnsiConsole.Ask<string>("[green]Enter a nickname for the new API key:[/] ");

            if (_settings.ApiKeys.Any(k => k.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine($"[red]An API key with the nickname '{nickname}' already exists.[/]");
                return Task.CompletedTask;
            }

            var apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>($"[green]Enter API key for '{nickname}':[/] ")
                    .PromptStyle("green")
                    .Secret());

            var newKey = new ApiKeySettings { Nickname = nickname, ApiKey = apiKey };
            _settings.ApiKeys.Add(newKey);
            _configService.SaveSettings(_settings);

            AnsiConsole.MarkupLine($"[green]API key '{nickname}' added successfully.[/]");
            
            if (AnsiConsole.Confirm($"[green]Do you want to make '{nickname}' the active key?[/]", false))
            {
                _settings.ActiveApiKeyNickname = nickname;
                _configService.SaveSettings(_settings);
                _initializeDisplay();
                AnsiConsole.MarkupLine($"[green]API key switched to '{nickname}'.[/]");
            }

            return Task.CompletedTask;
        }

        private Task HandleSettingsCommand(string args)
        {
            while (true)
            {
                var settingOptions = new List<string>
                {
                    $"User Data Path: {_settings.UserDataPath ?? "Not set"}",
                    $"Active API Key: {_settings.ActiveApiKeyNickname ?? "Not set"}",
                    $"Workspace: {_settings.Workspace ?? "Not set"}",
                    $"Default Model: {_settings.DefaultModel ?? "Not set"}",
                    "[red]Exit Settings[/]"
                };

                var selection = new SelectionPrompt<string>()
                    .Title("[green]Settings Configuration[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more settings)[/]")
                    .AddChoices(settingOptions);

                var selectedOption = AnsiConsole.Prompt(selection);

                if (selectedOption.Contains("Exit Settings"))
                {
                    break;
                }
                else if (selectedOption.StartsWith("User Data Path:"))
                {
                    EditUserDataPath();
                }
                else if (selectedOption.StartsWith("Active API Key:"))
                {
                    EditActiveApiKey();
                }
                else if (selectedOption.StartsWith("Workspace:"))
                {
                    EditWorkspace();
                }
                else if (selectedOption.StartsWith("Default Model:"))
                {
                    EditDefaultModel();
                }
            }

            return Task.CompletedTask;
        }

        private void EditUserDataPath()
        {
            var currentPath = _settings.UserDataPath ?? "";
            var newPath = AnsiConsole.Ask<string>($"[green]Enter User Data Path[/] [grey]({currentPath})[/]:", currentPath);
            
            if (!string.IsNullOrWhiteSpace(newPath) && newPath != currentPath)
            {
                _settings.UserDataPath = newPath;
                _configService.SaveSettings(_settings);
                AnsiConsole.MarkupLine($"[green]User Data Path updated to: {newPath}[/]");
            }
        }

        private void EditActiveApiKey()
        {
            if (_settings.ApiKeys.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No API keys available. Use /addkey to add one first.[/]");
                return;
            }

            var selection = new SelectionPrompt<string>()
                .Title("[green]Select Active API Key[/]")
                .PageSize(10);

            foreach (var key in _settings.ApiKeys)
            {
                var label = $"{key.Nickname} ({key.GetObfuscatedKey()})";
                if (key.Nickname == _settings.ActiveApiKeyNickname)
                {
                    label += " [cyan](current)[/]";
                }
                selection.AddChoice(label);
            }

            var selectedKeyLabel = AnsiConsole.Prompt(selection);
            var selectedNickname = selectedKeyLabel.Split(' ')[0];

            if (selectedNickname != _settings.ActiveApiKeyNickname)
            {
                _settings.ActiveApiKeyNickname = selectedNickname;
                _configService.SaveSettings(_settings);
                AnsiConsole.MarkupLine($"[green]Active API key changed to: {selectedNickname}[/]");
            }
        }

        private void EditWorkspace()
        {
            var currentWorkspace = _settings.Workspace ?? "";
            var newWorkspace = AnsiConsole.Ask<string>($"[green]Enter Workspace Path[/] [grey]({currentWorkspace})[/]:", currentWorkspace);
            
            if (!string.IsNullOrWhiteSpace(newWorkspace) && newWorkspace != currentWorkspace)
            {
                if (System.IO.Directory.Exists(newWorkspace))
                {
                    _settings.Workspace = newWorkspace;
                    _configService.SaveSettings(_settings);
                    AnsiConsole.MarkupLine($"[green]Workspace updated to: {newWorkspace}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Directory does not exist: {newWorkspace}[/]");
                }
            }
        }

        private void EditDefaultModel()
        {
            var selection = new SelectionPrompt<string>()
                .Title("[green]Select Default Model[/]")
                .PageSize(10);

            foreach (var model in _settings.AvailableModels)
            {
                var label = model;
                if (model == _settings.DefaultModel)
                {
                    label += " [cyan](current)[/]";
                }
                selection.AddChoice(label);
            }

            var selectedModel = AnsiConsole.Prompt(selection).Split(' ')[0];

            if (selectedModel != _settings.DefaultModel)
            {
                _settings.DefaultModel = selectedModel;
                _configService.SaveSettings(_settings);
                AnsiConsole.MarkupLine($"[green]Default model changed to: {selectedModel}[/]");
            }
        }

        private async Task HandleModeCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                ShowCurrentMode();
                return;
            }

            var parts = args.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var subCommand = parts[0].ToLowerInvariant();
            var subArgs = parts.Length > 1 ? parts[1] : string.Empty;

            switch (subCommand)
            {
                case "list":
                    ShowAvailableModes();
                    break;
                case "current":
                    ShowCurrentMode();
                    break;
                case "switch":
                    await HandleModeSwitchCommand(subArgs);
                    break;
                case "info":
                    ShowModeInfo(subArgs);
                    break;
                case "create":
                    await HandleModeCreateCommand(subArgs);
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]Unknown mode subcommand. Use: list, current, switch <mode-id>, info <mode-id>, or create[/]");
                    break;
            }
        }

        private void ShowCurrentMode()
        {
            var currentMode = _modeManager.GetCurrentMode();
            
            var panel = new Panel(new Rows(
                new Markup($"[bold green]Current Mode[/]"),
                new Markup($"[dim]ID:[/] [cyan]{currentMode.Id}[/]"),
                new Markup($"[dim]Name:[/] [cyan]{currentMode.Name}[/]"),
                new Markup($"[dim]Description:[/] [white]{currentMode.Description}[/]"),
                new Markup($"[dim]Type:[/] [cyan]{(currentMode.IsBuiltIn ? "Built-in" : "Custom")}[/]"),
                new Markup($"[dim]Allowed Tools:[/] [yellow]{currentMode.AllowedTools.Count}[/]")
            ))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

            AnsiConsole.Write(panel);
        }

        private void ShowAvailableModes()
        {
            var modes = _modeManager.GetAvailableModes();
            var currentMode = _modeManager.GetCurrentMode();
            
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.FromHex("#444444"))
                .AddColumn("[bold]ID[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Type[/]")
                .AddColumn("[bold]Tools[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var mode in modes.OrderBy(m => m.IsBuiltIn ? 0 : 1).ThenBy(m => m.Name))
            {
                var status = mode.Id == currentMode.Id ? "[green]Active[/]" : "[dim]Available[/]";
                var type = mode.IsBuiltIn ? "[cyan]Built-in[/]" : "[yellow]Custom[/]";
                
                table.AddRow(
                    mode.Id,
                    mode.Name,
                    type,
                    mode.AllowedTools.Count.ToString(),
                    status
                );
            }

            AnsiConsole.Write(table);
        }

        private async Task HandleModeSwitchCommand(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                AnsiConsole.MarkupLine("[red]Mode ID is required. Usage: /mode switch <mode-id>[/]");
                return;
            }

            var targetMode = _modeManager.GetMode(modeId);
            if (targetMode == null)
            {
                AnsiConsole.MarkupLine($"[red]Mode '{modeId}' not found. Use '/mode list' to see available modes.[/]");
                return;
            }

            var currentMode = _modeManager.GetCurrentMode();
            if (currentMode.Id == modeId)
            {
                AnsiConsole.MarkupLine($"[yellow]Already in mode '{targetMode.Name}'.[/]");
                return;
            }

            if (_modeManager.SetCurrentMode(modeId))
            {
                _chatSession.UpdateSystemPrompt();
                _initializeDisplay();
                AnsiConsole.MarkupLine($"[green]Successfully switched to mode '{targetMode.Name}'.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to switch to mode '{modeId}'.[/]");
            }
        }

        private void ShowModeInfo(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                AnsiConsole.MarkupLine("[red]Mode ID is required. Usage: /mode info <mode-id>[/]");
                return;
            }

            var mode = _modeManager.GetMode(modeId);
            if (mode == null)
            {
                AnsiConsole.MarkupLine($"[red]Mode '{modeId}' not found. Use '/mode list' to see available modes.[/]");
                return;
            }

            var toolsList = mode.AllowedTools.Count > 0
                ? string.Join(", ", mode.AllowedTools.Take(5)) + (mode.AllowedTools.Count > 5 ? $" (+{mode.AllowedTools.Count - 5} more)" : "")
                : "None";

            var panel = new Panel(new Rows(
                new Markup($"[bold cyan]{mode.Name}[/]"),
                new Markup($"[dim]ID:[/] [white]{mode.Id}[/]"),
                new Markup($"[dim]Type:[/] [cyan]{(mode.IsBuiltIn ? "Built-in" : "Custom")}[/]"),
                new Markup($"[dim]Description:[/]"),
                new Markup($"[white]{mode.Description}[/]"),
                new Markup($"[dim]Allowed Tools ({mode.AllowedTools.Count}):[/]"),
                new Markup($"[yellow]{toolsList}[/]"),
                !string.IsNullOrEmpty(mode.PreferredModel)
                    ? new Markup($"[dim]Preferred Model:[/] [cyan]{mode.PreferredModel}[/]")
                    : new Markup("[dim]Preferred Model:[/] [grey]Default[/]")
            ))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(1, 0);

            AnsiConsole.Write(panel);
        }

        private async Task HandleModeCreateCommand(string args)
        {
            if (!string.IsNullOrWhiteSpace(args))
            {
                await HandleModeCreateFromArgs(args);
            }
            else
            {
                await HandleInteractiveModeCreate();
            }
        }

        private async Task HandleModeCreateFromArgs(string args)
        {
            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 5)
            {
                AnsiConsole.MarkupLine("[red]Invalid arguments. Usage: /mode create <mode-id> <name> \"<description>\" \"<system-prompt>\" <tool1,tool2,tool3...>[/]");
                AnsiConsole.MarkupLine("[yellow]Example: /mode create writer \"Content Writer\" \"You are a writing assistant\" \"You help with content creation\" \"read_file,write_file,search_files\"[/]");
                return;
            }

            var modeId = parts[0];
            var name = parts[1];
            
            var quotedParts = StringParser.ExtractQuotedStrings(args);
            if (quotedParts.Count < 2)
            {
                AnsiConsole.MarkupLine("[red]Description and system prompt must be enclosed in quotes.[/]");
                return;
            }

            var description = quotedParts[0];
            var systemPrompt = quotedParts[1];
            
            var toolsPart = args.Split('"').Last().Trim();
            var tools = toolsPart.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            await CreateCustomMode(modeId, name, description, systemPrompt, tools);
        }

        private async Task HandleInteractiveModeCreate()
        {
            AnsiConsole.MarkupLine("[cyan]Create Custom Mode[/]");
            AnsiConsole.WriteLine();

            var modeId = PromptForModeId();
            if (string.IsNullOrEmpty(modeId)) return;

            var name = AnsiConsole.Ask<string>("[green]Enter mode name:[/] ");
            if (string.IsNullOrWhiteSpace(name))
            {
                AnsiConsole.MarkupLine("[red]Mode name cannot be empty.[/]");
                return;
            }

            var description = AnsiConsole.Ask<string>("[green]Enter mode description:[/] ");
            if (string.IsNullOrWhiteSpace(description))
            {
                AnsiConsole.MarkupLine("[red]Mode description cannot be empty.[/]");
                return;
            }

            var systemPrompt = PromptForSystemPrompt();
            if (string.IsNullOrEmpty(systemPrompt)) return;

            var tools = PromptForTools();
            if (tools == null) return;

            await CreateCustomMode(modeId, name, description, systemPrompt, tools);
        }

        private string PromptForModeId()
        {
            while (true)
            {
                var modeId = AnsiConsole.Ask<string>("[green]Enter mode ID (alphanumeric, no spaces):[/] ");
                
                if (string.IsNullOrWhiteSpace(modeId))
                {
                    AnsiConsole.MarkupLine("[red]Mode ID cannot be empty.[/]");
                    continue;
                }

                if (!Regex.IsMatch(modeId, @"^[a-zA-Z0-9_-]+$"))
                {
                    AnsiConsole.MarkupLine("[red]Mode ID can only contain alphanumeric characters, hyphens, and underscores.[/]");
                    continue;
                }

                if (_modeManager.GetMode(modeId) != null)
                {
                    AnsiConsole.MarkupLine($"[red]Mode '{modeId}' already exists. Please choose a different ID.[/]");
                    continue;
                }

                return modeId;
            }
        }

        private string PromptForSystemPrompt()
        {
            AnsiConsole.MarkupLine("[green]Enter system prompt (press Enter twice to finish):[/]");
            var lines = new List<string>();
            var emptyLineCount = 0;

            while (emptyLineCount < 2)
            {
                var line = System.Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    emptyLineCount++;
                }
                else
                {
                    emptyLineCount = 0;
                    lines.Add(line);
                }
            }

            var systemPrompt = string.Join(Environment.NewLine, lines).Trim();
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                AnsiConsole.MarkupLine("[red]System prompt cannot be empty.[/]");
                return string.Empty;
            }

            return systemPrompt;
        }

        private List<string> PromptForTools()
        {
            var availableTools = _toolRegistry.GetAllTools();
            var categories = availableTools.Select(t => t.Category).Distinct().Where(c => !string.IsNullOrEmpty(c)).OrderBy(c => c).ToList();
            var tags = availableTools.SelectMany(t => t.Tags).Distinct().Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t).ToList();

            while (true)
            {
                var selectionMethod = new SelectionPrompt<string>()
                    .Title("[green]How would you like to select tools?[/]")
                    .AddChoices(
                        "Select individual tools",
                        "Select by categories",
                        "Select by tags",
                        "Select all tools",
                        "Advanced selection"
                    );

                var method = AnsiConsole.Prompt(selectionMethod);
                List<string> result;

                switch (method)
                {
                    case "Select individual tools":
                        result = SelectIndividualToolsWithNavigation(availableTools);
                        break;
                    case "Select by categories":
                        result = SelectToolsByCategoriesWithNavigation(categories);
                        break;
                    case "Select by tags":
                        result = SelectToolsByTagsWithNavigation(tags);
                        break;
                    case "Select all tools":
                        return availableTools.Select(t => t.Name).ToList();
                    case "Advanced selection":
                        result = AdvancedToolSelection(availableTools, categories, tags);
                        break;
                    default:
                        result = SelectIndividualToolsWithNavigation(availableTools);
                        break;
                }

                if (result != null)
                {
                    return result;
                }
            }
        }

        private List<string> SelectIndividualToolsWithNavigation(List<ToolTypeInfo> availableTools)
        {
            while (true)
            {
                ShowToolsTable(availableTools);

                var toolChoices = new List<string> { "[yellow]← Go back to selection method[/]" };
                toolChoices.AddRange(availableTools.Select(t => t.Name));

                var toolSelection = new MultiSelectionPrompt<string>()
                    .Title("[green]Select tools for this mode:[/]")
                    .PageSize(15)
                    .MoreChoicesText("[grey](Move up and down to reveal more tools)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle tools, [green]<enter>[/] to accept, select [yellow]← Go back[/] to return)[/]")
                    .AddChoices(toolChoices);

                var selectedItems = AnsiConsole.Prompt(toolSelection);

                if (selectedItems.Contains("[yellow]← Go back to selection method[/]"))
                {
                    return null;
                }

                if (selectedItems.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]At least one tool must be selected.[/]");
                    var retry = AnsiConsole.Confirm("[yellow]Would you like to try again?[/]");
                    if (!retry)
                    {
                        return null;
                    }
                    continue;
                }

                return selectedItems.ToList();
            }
        }

        private List<string> SelectIndividualTools(List<ToolTypeInfo> availableTools)
        {
            ShowToolsTable(availableTools);

            var toolSelection = new MultiSelectionPrompt<string>()
                .Title("[green]Select tools for this mode (use spacebar to select, enter to confirm):[/]")
                .PageSize(15)
                .MoreChoicesText("[grey](Move up and down to reveal more tools)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a tool, [green]<enter>[/] to accept)[/]")
                .AddChoices(availableTools.Select(t => t.Name));

            var selectedTools = AnsiConsole.Prompt(toolSelection);

            if (selectedTools.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]At least one tool must be selected.[/]");
                return null;
            }

            return selectedTools.ToList();
        }

        private List<string> SelectToolsByCategoriesWithNavigation(List<string> categories)
        {
            while (true)
            {
                if (categories.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tool categories available.[/]");
                    return null;
                }

                var options = new List<string>
                {
                    "Show available categories",
                    "Select categories",
                    "[yellow]← Go back to selection method[/]"
                };

                var action = new SelectionPrompt<string>()
                    .Title("[cyan]Category Selection[/]")
                    .AddChoices(options);

                var choice = AnsiConsole.Prompt(action);

                if (choice.Contains("Go back"))
                {
                    return null;
                }
                else if (choice.Contains("Show available categories"))
                {
                    ShowCategoriesTable(categories);
                    continue;
                }
                else if (choice.Contains("Select categories"))
                {
                    var categorySelection = new MultiSelectionPrompt<string>()
                        .Title("[green]Select categories to include all their tools:[/]")
                        .PageSize(10)
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a category, [green]<enter>[/] to accept)[/]")
                        .AddChoices(categories);

                    var selectedCategories = AnsiConsole.Prompt(categorySelection);

                    if (selectedCategories.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]At least one category must be selected.[/]");
                        var retry = AnsiConsole.Confirm("[yellow]Would you like to try again?[/]");
                        if (!retry)
                        {
                            return null;
                        }
                        continue;
                    }

                    var selectedTools = new List<string>();
                    foreach (var category in selectedCategories)
                    {
                        var toolsInCategory = _toolRegistry.GetToolsByCategory(category);
                        selectedTools.AddRange(toolsInCategory.Select(t => t.Name));
                    }

                    AnsiConsole.MarkupLine($"[green]Selected {selectedTools.Count} tools from {selectedCategories.Count} categories.[/]");
                    return selectedTools.Distinct().ToList();
                }
            }
        }

        private List<string> SelectToolsByCategories(List<string> categories)
        {
            if (categories.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No tool categories available.[/]");
                return SelectIndividualTools(_toolRegistry.GetAllTools());
            }

            var categorySelection = new MultiSelectionPrompt<string>()
                .Title("[green]Select categories to include all their tools:[/]")
                .PageSize(10)
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a category, [green]<enter>[/] to accept)[/]")
                .AddChoices(categories);

            var selectedCategories = AnsiConsole.Prompt(categorySelection);

            if (selectedCategories.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]At least one category must be selected.[/]");
                return null;
            }

            var selectedTools = new List<string>();
            foreach (var category in selectedCategories)
            {
                var toolsInCategory = _toolRegistry.GetToolsByCategory(category);
                selectedTools.AddRange(toolsInCategory.Select(t => t.Name));
            }

            AnsiConsole.MarkupLine($"[green]Selected {selectedTools.Count} tools from {selectedCategories.Count} categories.[/]");
            return selectedTools.Distinct().ToList();
        }

        private List<string> SelectToolsByTagsWithNavigation(List<string> tags)
        {
            while (true)
            {
                if (tags.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tool tags available.[/]");
                    return null;
                }

                var options = new List<string>
                {
                    "Show available tags",
                    "Select tags",
                    "[yellow]← Go back to selection method[/]"
                };

                var action = new SelectionPrompt<string>()
                    .Title("[cyan]Tag Selection[/]")
                    .AddChoices(options);

                var choice = AnsiConsole.Prompt(action);

                if (choice.Contains("Go back"))
                {
                    return null;
                }
                else if (choice.Contains("Show available tags"))
                {
                    ShowTagsTable(tags);
                    continue;
                }
                else if (choice.Contains("Select tags"))
                {
                    var tagSelection = new MultiSelectionPrompt<string>()
                        .Title("[green]Select tags to include all tools with those tags:[/]")
                        .PageSize(10)
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a tag, [green]<enter>[/] to accept)[/]")
                        .AddChoices(tags);

                    var selectedTags = AnsiConsole.Prompt(tagSelection);

                    if (selectedTags.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]At least one tag must be selected.[/]");
                        var retry = AnsiConsole.Confirm("[yellow]Would you like to try again?[/]");
                        if (!retry)
                        {
                            return null;
                        }
                        continue;
                    }

                    var selectedTools = new List<string>();
                    foreach (var tag in selectedTags)
                    {
                        var toolsWithTag = _toolRegistry.GetToolsByTag(tag);
                        selectedTools.AddRange(toolsWithTag.Select(t => t.Name));
                    }

                    AnsiConsole.MarkupLine($"[green]Selected {selectedTools.Count} tools with {selectedTags.Count} tags.[/]");
                    return selectedTools.Distinct().ToList();
                }
            }
        }

        private List<string> SelectToolsByTags(List<string> tags)
        {
            if (tags.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No tool tags available.[/]");
                return SelectIndividualTools(_toolRegistry.GetAllTools());
            }

            var tagSelection = new MultiSelectionPrompt<string>()
                .Title("[green]Select tags to include all tools with those tags:[/]")
                .PageSize(10)
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a tag, [green]<enter>[/] to accept)[/]")
                .AddChoices(tags);

            var selectedTags = AnsiConsole.Prompt(tagSelection);

            if (selectedTags.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]At least one tag must be selected.[/]");
                return null;
            }

            var selectedTools = new List<string>();
            foreach (var tag in selectedTags)
            {
                var toolsWithTag = _toolRegistry.GetToolsByTag(tag);
                selectedTools.AddRange(toolsWithTag.Select(t => t.Name));
            }

            AnsiConsole.MarkupLine($"[green]Selected {selectedTools.Count} tools with {selectedTags.Count} tags.[/]");
            return selectedTools.Distinct().ToList();
        }

        private List<string> AdvancedToolSelection(List<ToolTypeInfo> availableTools, List<string> categories, List<string> tags)
        {
            var selectedTools = new HashSet<string>();

            while (true)
            {
                var options = new List<string>
                {
                    $"Add individual tools ({selectedTools.Count} selected)",
                    $"Add by categories",
                    $"Add by tags",
                    $"Remove individual tools",
                    $"Show selected tools",
                    "[green]Finish selection[/]",
                    "[yellow]← Go back to selection method[/]"
                };

                var action = new SelectionPrompt<string>()
                    .Title($"[cyan]Advanced Tool Selection - {selectedTools.Count} tools selected[/]")
                    .AddChoices(options);

                var choice = AnsiConsole.Prompt(action);

                if (choice.Contains("Go back"))
                {
                    return null;
                }
                else if (choice.Contains("Finish selection"))
                {
                    break;
                }
                else if (choice.Contains("Show selected tools"))
                {
                    if (selectedTools.Any())
                    {
                        AnsiConsole.MarkupLine($"[cyan]Selected tools ({selectedTools.Count}):[/]");
                        foreach (var tool in selectedTools.OrderBy(t => t))
                        {
                            AnsiConsole.MarkupLine($"  • [green]{tool}[/]");
                        }
                        AnsiConsole.WriteLine();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]No tools selected yet.[/]");
                    }
                    continue;
                }
                else if (choice.Contains("Add individual tools"))
                {
                    var remainingTools = availableTools.Where(t => !selectedTools.Contains(t.Name)).ToList();
                    if (remainingTools.Any())
                    {
                        var newTools = SelectIndividualTools(remainingTools);
                        if (newTools != null)
                        {
                            foreach (var tool in newTools)
                            {
                                selectedTools.Add(tool);
                            }
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]All tools are already selected.[/]");
                    }
                }
                else if (choice.Contains("Add by categories"))
                {
                    var newTools = SelectToolsByCategories(categories);
                    if (newTools != null)
                    {
                        foreach (var tool in newTools)
                        {
                            selectedTools.Add(tool);
                        }
                    }
                }
                else if (choice.Contains("Add by tags"))
                {
                    var newTools = SelectToolsByTags(tags);
                    if (newTools != null)
                    {
                        foreach (var tool in newTools)
                        {
                            selectedTools.Add(tool);
                        }
                    }
                }
                else if (choice.Contains("Remove individual tools"))
                {
                    if (selectedTools.Any())
                    {
                        var removeSelection = new MultiSelectionPrompt<string>()
                            .Title("[red]Select tools to remove:[/]")
                            .PageSize(15)
                            .InstructionsText("[grey](Press [blue]<space>[/] to toggle a tool, [green]<enter>[/] to accept)[/]")
                            .AddChoices(selectedTools);

                        var toolsToRemove = AnsiConsole.Prompt(removeSelection);
                        foreach (var tool in toolsToRemove)
                        {
                            selectedTools.Remove(tool);
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]No tools selected to remove.[/]");
                    }
                }
            }

            if (selectedTools.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]At least one tool must be selected.[/]");
                return null;
            }

            return selectedTools.ToList();
        }

        private void ShowToolsTable(List<ToolTypeInfo> tools)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.FromHex("#444444"))
                .AddColumn("[bold]Tool Name[/]")
                .AddColumn("[bold]Category[/]")
                .AddColumn("[bold]Tags[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var tool in tools.OrderBy(t => t.Category).ThenBy(t => t.Name))
            {
                var tags = tool.Tags.Any() ? string.Join(", ", tool.Tags) : "[dim]none[/]";
                var description = ToolDescriptions.Descriptions.GetValueOrDefault(tool.Name, "[dim]No description[/]");
                
                table.AddRow(
                    tool.Name,
                    tool.Category ?? "[dim]uncategorized[/]",
                    tags,
                    description
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private void ShowCategoriesTable(List<string> categories)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.FromHex("#444444"))
                .AddColumn("[bold]Category[/]")
                .AddColumn("[bold]Tool Count[/]")
                .AddColumn("[bold]Example Tools[/]");

            foreach (var category in categories.OrderBy(c => c))
            {
                var toolsInCategory = _toolRegistry.GetToolsByCategory(category);
                var exampleTools = toolsInCategory.Take(3).Select(t => t.Name);
                var exampleText = string.Join(", ", exampleTools);
                if (toolsInCategory.Count > 3)
                {
                    exampleText += $" + {toolsInCategory.Count - 3} more";
                }

                table.AddRow(
                    category,
                    toolsInCategory.Count.ToString(),
                    exampleText
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private void ShowTagsTable(List<string> tags)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.FromHex("#444444"))
                .AddColumn("[bold]Tag[/]")
                .AddColumn("[bold]Tool Count[/]")
                .AddColumn("[bold]Example Tools[/]");

            foreach (var tag in tags.OrderBy(t => t))
            {
                var toolsWithTag = _toolRegistry.GetToolsByTag(tag);
                var exampleTools = toolsWithTag.Take(3).Select(t => t.Name);
                var exampleText = string.Join(", ", exampleTools);
                if (toolsWithTag.Count > 3)
                {
                    exampleText += $" + {toolsWithTag.Count - 3} more";
                }

                table.AddRow(
                    tag,
                    toolsWithTag.Count.ToString(),
                    exampleText
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private async Task CreateCustomMode(string modeId, string name, string description, string systemPrompt, List<string> tools)
        {
            var availableToolNames = _toolRegistry.GetAllTools().Select(t => t.Name).ToList();
            var invalidTools = tools.Where(t => !availableToolNames.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
            
            if (invalidTools.Any())
            {
                AnsiConsole.MarkupLine($"[red]Invalid tools: {string.Join(", ", invalidTools)}[/]");
                AnsiConsole.MarkupLine($"[yellow]Available tools: {string.Join(", ", availableToolNames)}[/]");
                return;
            }

            var mode = new ModeBuilder()
                .WithId(modeId)
                .WithName(name)
                .WithDescription(description)
                .WithSystemPrompt(systemPrompt)
                .AllowTools(tools.ToArray())
                .Build();

            if (_modeManager.AddCustomMode(mode))
            {
                var panel = new Panel(new Rows(
                    new Markup($"[bold green]Custom Mode Created Successfully[/]"),
                    new Markup($"[dim]ID:[/] [cyan]{mode.Id}[/]"),
                    new Markup($"[dim]Name:[/] [cyan]{mode.Name}[/]"),
                    new Markup($"[dim]Description:[/] [white]{mode.Description}[/]"),
                    new Markup($"[dim]System Prompt:[/] [white]{mode.SystemPrompt.Substring(0, Math.Min(100, mode.SystemPrompt.Length))}{(mode.SystemPrompt.Length > 100 ? "..." : "")}[/]"),
                    new Markup($"[dim]Tools ({mode.AllowedTools.Count}):[/] [yellow]{string.Join(", ", mode.AllowedTools)}[/]")
                ))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
                .Padding(1, 0);

                AnsiConsole.Write(panel);

                if (AnsiConsole.Confirm($"[green]Do you want to switch to '{mode.Name}' mode now?[/]", false))
                {
                    if (_modeManager.SetCurrentMode(modeId))
                    {
                        _chatSession.UpdateSystemPrompt();
                        _initializeDisplay();
                        AnsiConsole.MarkupLine($"[green]Successfully switched to mode '{mode.Name}'.[/]");
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to create mode '{modeId}'. Mode may already exist.[/]");
            }
        }

        private async Task HandleTestCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                AnsiConsole.MarkupLine("[yellow]Usage: /test <subcommand>[/]");
                AnsiConsole.MarkupLine("[cyan]Available subcommands:[/]");
                AnsiConsole.MarkupLine("  • [green]tool[/] - Test tool discovery and tag assignment");
                return;
            }

            var parts = args.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var subCommand = parts[0].ToLowerInvariant();

            switch (subCommand)
            {
                case "tool":
                case "tools":
                    await HandleTestToolsCommand(parts.Length > 1 ? parts[1] : string.Empty);
                    break;
                default:
                    AnsiConsole.MarkupLine($"[red]Unknown test subcommand: {subCommand}[/]");
                    AnsiConsole.MarkupLine("[yellow]Available subcommands: tool[/]");
                    break;
            }
        }

        private Task HandleTestToolsCommand(string args)
        {
            AnsiConsole.MarkupLine("[cyan]Tool Discovery Test[/]");
            AnsiConsole.WriteLine();

            var allTools = _toolRegistry.GetAllTools();
            AnsiConsole.MarkupLine($"[green]Total tools discovered: {allTools.Count}[/]");
            AnsiConsole.WriteLine();

            var toolsWithTags = allTools.Where(t => t.Tags.Any()).ToList();
            AnsiConsole.MarkupLine($"[yellow]Tools with tags: {toolsWithTags.Count}[/]");
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Debug - All Tool Details:[/]");
            foreach (var tool in allTools.Take(5).OrderBy(t => t.Name))
            {
                AnsiConsole.MarkupLine($"  • [white]{tool.Name}[/]");
                AnsiConsole.MarkupLine($"    Category: [yellow]{tool.Category ?? "null"}[/]");
                AnsiConsole.MarkupLine($"    Tags Count: [cyan]{tool.Tags.Count}[/]");
                if (tool.Tags.Any())
                {
                    AnsiConsole.MarkupLine($"    Tags: [cyan]{string.Join(", ", tool.Tags)}[/]");
                }
                AnsiConsole.MarkupLine($"    Priority: [white]{tool.Priority}[/]");
                AnsiConsole.WriteLine();
            }
            
            if (toolsWithTags.Any())
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.FromHex("#444444"))
                    .AddColumn("[bold]Tool Name[/]")
                    .AddColumn("[bold]Category[/]")
                    .AddColumn("[bold]Tags[/]")
                    .AddColumn("[bold]Priority[/]");

                foreach (var tool in toolsWithTags.OrderBy(t => t.Name))
                {
                    table.AddRow(
                        tool.Name,
                        tool.Category ?? "[dim]none[/]",
                        $"[cyan]{string.Join(", ", tool.Tags)}[/]",
                        tool.Priority.ToString()
                    );
                }

                AnsiConsole.Write(table);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Tag Summary:[/]");
            
            var allTags = allTools.SelectMany(t => t.Tags).Distinct().OrderBy(t => t).ToList();
            if (allTags.Any())
            {
                foreach (var tag in allTags)
                {
                    var toolsWithTag = _toolRegistry.GetToolsByTag(tag);
                    AnsiConsole.MarkupLine($"  • [green]{tag}[/]: {toolsWithTag.Count} tools");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("  [dim]No tags found[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Category Summary:[/]");
            
            var allCategories = allTools.Select(t => t.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
            foreach (var category in allCategories)
            {
                var toolsInCategory = _toolRegistry.GetToolsByCategory(category);
                AnsiConsole.MarkupLine($"  • [yellow]{category}[/]: {toolsInCategory.Count} tools");
            }

            var createIssueTool = allTools.FirstOrDefault(t => t.Name == "create_github_issue");
            if (createIssueTool != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[cyan]CreateIssueTool Details:[/]");
                AnsiConsole.MarkupLine($"  Name: [green]{createIssueTool.Name}[/]");
                AnsiConsole.MarkupLine($"  Category: [yellow]{createIssueTool.Category ?? "null"}[/]");
                AnsiConsole.MarkupLine($"  Tags Count: [cyan]{createIssueTool.Tags.Count}[/]");
                if (createIssueTool.Tags.Any())
                {
                    AnsiConsole.MarkupLine($"  Tags: [cyan]{string.Join(", ", createIssueTool.Tags)}[/]");
                    AnsiConsole.MarkupLine($"  Has 'github' tag: [green]{createIssueTool.Tags.Contains("github")}[/]");
                    AnsiConsole.MarkupLine($"  Has 'create' tag: [green]{createIssueTool.Tags.Contains("create")}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("  [red]No tags found for this tool[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]CreateIssueTool not found![/]");
            }

            return Task.CompletedTask;
        }

        private async Task HandleBackupsCommand(string args)
        {
            try
            {


                var parts = args?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                
                string action;
                string? filePath = null;
                string? backupId = null;
                int? limit = null;
                int? retentionDays = null;
                
                if (parts.Length == 0)
                {

                    action = "list";
                }
                else if (parts.Length == 1 && !IsValidAction(parts[0]))
                {

                    action = "list";
                    filePath = parts[0];
                }
                else
                {

                    action = parts[0].ToLower();
                    
                    if (action == "list" && parts.Length > 1)
                    {
                        filePath = parts[1];
                        if (parts.Length > 2 && int.TryParse(parts[2], out var limitValue))
                        {
                            limit = limitValue;
                        }
                    }
                    else if (action == "restore" && parts.Length > 1)
                    {
                        backupId = parts[1];
                        if (parts.Length > 2)
                        {
                            filePath = parts[2];
                        }
                    }
                    else if (action == "diff" && parts.Length > 1)
                    {
                        backupId = parts[1];
                        if (parts.Length > 2)
                        {
                            filePath = parts[2];
                        }
                    }
                    else if (action == "cleanup")
                    {
                        if (parts.Length > 1 && int.TryParse(parts[1], out var retentionValue))
                        {
                            retentionDays = retentionValue;
                        }
                    }
                    else if (!IsValidAction(action))
                    {
                        AnsiConsole.MarkupLine("[red]Invalid action. Use: list, restore, diff, cleanup, or status[/]");
                        AnsiConsole.MarkupLine("[yellow]Examples:[/]");
                        AnsiConsole.MarkupLine("  /backups                     - List all backups");
                        AnsiConsole.MarkupLine("  /backups Program.cs          - List backups for Program.cs");
                        AnsiConsole.MarkupLine("  /backups list Program.cs     - List backups for Program.cs");
                        AnsiConsole.MarkupLine("  /backups status              - Show backup system status");
                        AnsiConsole.MarkupLine("  /backups restore <backup-id> - Restore a backup");
                        AnsiConsole.MarkupLine("  /backups diff <backup-id>    - Show backup differences");
                        AnsiConsole.MarkupLine("  /backups cleanup [days]      - Cleanup old backups");
                        return;
                    }
                }
                
                var commandArgs = new { action, filePath, backupId, limit, retentionDays };
                var json = System.Text.Json.JsonSerializer.Serialize(commandArgs);
                
                var backupTool = new LogiQCLI.Tools.Core.BackupCommandsTool();
                var result = await backupTool.Execute(json);
                AnsiConsole.WriteLine(result);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error executing backup command: {ex.Message}[/]");
            }
        }
        
        private bool IsValidAction(string action)
        {
            var validActions = new[] { "list", "restore", "diff", "cleanup", "status" };
            return validActions.Contains(action.ToLower());
        }

        private async Task HandleRestoreCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                AnsiConsole.MarkupLine("[yellow]Usage: /restore <backup-id> [target-path][/]");
                return;
            }

            try
            {
                var parts = args.Trim().Split(' ', 2);
                var backupId = parts[0];
                var targetPath = parts.Length > 1 ? parts[1] : (string?)null;

                var commandArgs = new { action = "restore", backupId, filePath = targetPath };
                var json = System.Text.Json.JsonSerializer.Serialize(commandArgs);
                var backupTool = new LogiQCLI.Tools.Core.BackupCommandsTool();
                var result = await backupTool.Execute(json);
                AnsiConsole.WriteLine(result);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error restoring backup: {ex.Message}[/]");
            }
        }

        private async Task HandleBackupDiffCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                AnsiConsole.MarkupLine("[yellow]Usage: /backup-diff <backup-id> [compare-file-path][/]");
                return;
            }

            try
            {
                var parts = args.Trim().Split(' ', 2);
                var backupId = parts[0];
                var comparePath = parts.Length > 1 ? parts[1] : (string?)null;

                var commandArgs = new { action = "diff", backupId, filePath = comparePath };
                var json = System.Text.Json.JsonSerializer.Serialize(commandArgs);
                var backupTool = new LogiQCLI.Tools.Core.BackupCommandsTool();
                var result = await backupTool.Execute(json);
                AnsiConsole.WriteLine(result);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error showing backup diff: {ex.Message}[/]");
            }
        }

        private async Task HandleBackupCleanupCommand(string args)
        {
            try
            {
                int? retentionDays = null;
                if (!string.IsNullOrWhiteSpace(args) && int.TryParse(args.Trim(), out var days))
                {
                    retentionDays = days;
                }

                var commandArgs = new { action = "cleanup", retentionDays = (object?)retentionDays };
                var json = System.Text.Json.JsonSerializer.Serialize(commandArgs);
                var backupTool = new LogiQCLI.Tools.Core.BackupCommandsTool();
                var result = await backupTool.Execute(json);
                AnsiConsole.WriteLine(result);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error cleaning up backups: {ex.Message}[/]");
            }
        }

        private async Task HandleBackupStatusCommand(string args)
        {
            try
            {
                var commandArgs = new { action = "status" };
                var json = System.Text.Json.JsonSerializer.Serialize(commandArgs);
                var backupTool = new LogiQCLI.Tools.Core.BackupCommandsTool();
                var result = await backupTool.Execute(json);
                AnsiConsole.WriteLine(result);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error showing backup status: {ex.Message}[/]");
            }
        }

    }
}
