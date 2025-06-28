using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.Core;
using LogiQCLI.Tools.FileOperations;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.SystemOperations;
using LogiQCLI.Tools.GitHub;
using LogiQCLI.Presentation.Console;
using LogiQCLI.Presentation.Console.Components;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Commands.Core.Interfaces;
using Spectre.Console;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tests.Core;
using System.Text;
using LogiQCLI.Infrastructure.Providers;
using System.Net.Http;

public class Program
{
    private static IServiceContainer? _serviceContainer;
    private static IToolRegistry? _toolRegistry;
    private static IToolFactory? _toolFactory;
    private static IToolDiscoveryService? _toolDiscoveryService;

    public static async Task Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            if (args.Contains("-test") || args.Contains("--test"))
            {
                await RunTestModeAsync(args);
                return;
            }
            
            var configService = new ConfigurationService();
            var dataDirectory = configService.GetDataDirectory();
            var configFilePath = Path.Combine(dataDirectory, "settings.json");
            var migratedFlagPath = Path.Combine(dataDirectory, "migrated.flag");

            if (File.Exists(configFilePath) && !File.Exists(migratedFlagPath))
            {
                File.Delete(configFilePath);
            }

            var settings = configService.LoadSettings();
            var isNewSettings = false;

            if (settings == null)
            {
                var configManager = new InteractiveConfigurationManager();
                settings = configManager.ConfigureInteractively();
                settings.UserDataPath = configService.GetDataDirectory();
                configService.SaveSettings(settings);
                File.WriteAllText(migratedFlagPath, "migrated");
                isNewSettings = true;
            }

            if (!isNewSettings)
            {
                PromptForWorkspace(settings, configService);
            }

            ValidateEnvironment(settings);
            if (settings.Workspace != null)
            {
                Directory.SetCurrentDirectory(settings.Workspace);
            }

            InitializeServices(settings);
            
            var configurationService = _serviceContainer.GetService<ConfigurationService>()!;

            var toolRegistry = _toolRegistry ?? throw new InvalidOperationException("Tool registry not initialized.");
            var modeManager = new ModeManager(configurationService, toolRegistry);
            _serviceContainer.RegisterInstance<IModeManager>(modeManager);
            
            var fileReadRegistry = _serviceContainer.GetService<LogiQCLI.Presentation.Console.Session.FileReadRegistry>()!;
            var chatSession = new ChatSession(settings.DefaultModel ?? "default", modeManager, fileReadRegistry);
            _serviceContainer.RegisterInstance(chatSession);

            InitializeToolSystem();
            
            if (_toolRegistry == null)
            {
                throw new InvalidOperationException("Tool registry not initialized.");
            }

            var toolHandler = CreateToolHandler(modeManager, settings);
            var commandHandler = CreateCommandHandler(settings, configService, modeManager);

            var metadataService = _serviceContainer.GetService<LogiQCLI.Infrastructure.ApiClients.OpenRouter.ModelMetadataService>()!;
            var chatUI = new ChatInterface(_serviceContainer!, toolHandler, settings, configService, modeManager, _toolRegistry!, commandHandler, chatSession, fileReadRegistry, metadataService);
            await chatUI.RunAsync();
        }
        catch (Exception ex)
        {
            RenderError($"Fatal error: {ex.Message}");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    private static void ValidateEnvironment(ApplicationSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Workspace) || !Directory.Exists(settings.Workspace))
        {
            throw new InvalidOperationException($"Workspace directory does not exist: {settings.Workspace}");
        }

        var activeApiKey = settings.GetActiveApiKey();
        if (activeApiKey == null || string.IsNullOrEmpty(activeApiKey.ApiKey))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: No active API key is configured. Use /settings to configure one.[/]");
        }
    }

    private static void InitializeServices(ApplicationSettings settings)
    {
        _serviceContainer = new ServiceContainer();
        
        _serviceContainer.RegisterInstance(settings);

        _serviceContainer.RegisterFactory<HttpClient>(container => new HttpClient { Timeout = TimeSpan.FromSeconds(300) });
        
        var httpClient = _serviceContainer.GetService<HttpClient>();
        var apiKey = settings.GetActiveApiKey()?.ApiKey ?? "dummy-key-for-startup";
        
        _serviceContainer.RegisterFactory<OpenRouterClient>(container =>
            new OpenRouterClient(
                httpClient,
                apiKey,
                settings.CacheStrategy
            )
        );

        var gitHubClient = new GitHubClientWrapper(settings.GitHub?.Token);
        _serviceContainer.RegisterInstance(gitHubClient);
        
        var documentationApiClient = new LogiQCLI.Infrastructure.ApiClients.Documentation.DocumentationApiClient(httpClient);
        _serviceContainer.RegisterInstance(documentationApiClient);
        
        var tavilyApiKey = settings.Tavily?.ApiKey ?? "dummy-key-for-startup";
        var tavilyClient = new LogiQCLI.Infrastructure.ApiClients.Tavily.TavilyClient(httpClient, tavilyApiKey, settings.Tavily?.BaseUrl);
        _serviceContainer.RegisterInstance(tavilyClient);
        
        _serviceContainer.RegisterInstance<IServiceContainer>(_serviceContainer);
        
        _toolRegistry = new ToolRegistry();
        _serviceContainer.RegisterInstance<IToolRegistry>(_toolRegistry);
        
        _toolFactory = new ToolFactory(_serviceContainer);
        _serviceContainer.RegisterInstance<IToolFactory>(_toolFactory);
        
        _toolDiscoveryService = new ToolDiscoveryService();
        _serviceContainer.RegisterInstance<IToolDiscoveryService>(_toolDiscoveryService);
        
        var commandRegistry = new LogiQCLI.Commands.Core.CommandRegistry();
        _serviceContainer.RegisterInstance<LogiQCLI.Commands.Core.Interfaces.ICommandRegistry>(commandRegistry);
        
        var commandFactory = new LogiQCLI.Commands.Core.CommandFactory(_serviceContainer);
        _serviceContainer.RegisterInstance<LogiQCLI.Commands.Core.Interfaces.ICommandFactory>(commandFactory);
        
        var commandDiscoveryService = new LogiQCLI.Commands.Core.CommandDiscoveryService();
        _serviceContainer.RegisterInstance<LogiQCLI.Commands.Core.Interfaces.ICommandDiscoveryService>(commandDiscoveryService);
        
        var configService = new ConfigurationService();
        _serviceContainer.RegisterInstance(configService);
        
        _serviceContainer.RegisterFactory<IDisplayService>(container => 
        {
            var appSettings = container.GetService<ApplicationSettings>();
            var modeManager = container.GetService<IModeManager>();
            return new DisplayService(appSettings, modeManager);
        });
        
        _serviceContainer.RegisterFactory<Action>(container =>
        {
            var displayService = container.GetService<IDisplayService>();
            return displayService.GetInitializeDisplayAction();
        });

        var fileReadRegistry = new LogiQCLI.Presentation.Console.Session.FileReadRegistry();
        _serviceContainer.RegisterInstance(fileReadRegistry);

        _serviceContainer.RegisterSingleton<LogiQCLI.Infrastructure.ApiClients.OpenRouter.ModelMetadataService, LogiQCLI.Infrastructure.ApiClients.OpenRouter.ModelMetadataService>();
        _serviceContainer.RegisterSingleton<IModelDiscoveryService, ModelDiscoveryService>();
    }

    private static void InitializeToolSystem()
    {
        if (_toolDiscoveryService == null || _toolRegistry == null || _toolFactory == null)
        {
            return;
        }

        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var discoveredTools = _toolDiscoveryService.DiscoverTools(currentAssembly);
            
            AnsiConsole.MarkupLine($"[cyan]Discovered {discoveredTools.Count} tools[/]");
            
            var registeredCount = 0;
            var skippedCount = 0;
            
            foreach (var toolInfo in discoveredTools.OrderBy(t => t.Priority).ThenBy(t => t.Name))
            {
                try
                {
                    if (_toolFactory.CanCreateTool(toolInfo))
                    {
                        _toolRegistry.RegisterTool(toolInfo);
                        registeredCount++;
                    }
                    else
                    {
                        skippedCount++;
                        AnsiConsole.MarkupLine($"[yellow]Skipped tool '{toolInfo.Name}' - dependencies not met[/]");
                    }
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    AnsiConsole.MarkupLine($"[red]Failed to register tool '{toolInfo.Name}': {ex.Message}[/]");
                }
            }
            
            AnsiConsole.MarkupLine($"[green]Successfully registered {registeredCount} tools[/]");
            if (skippedCount > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipped {skippedCount} tools[/]");
            }
            
            InitializeCommandSystem();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Tool discovery failed: {ex.Message}[/]");
        }
    }
    
    private static void InitializeCommandSystem()
    {
        var commandRegistry = _serviceContainer?.GetService<LogiQCLI.Commands.Core.Interfaces.ICommandRegistry>();
        var commandDiscoveryService = _serviceContainer?.GetService<LogiQCLI.Commands.Core.Interfaces.ICommandDiscoveryService>();
        var commandFactory = _serviceContainer?.GetService<LogiQCLI.Commands.Core.Interfaces.ICommandFactory>();
        
        if (commandDiscoveryService == null || commandRegistry == null || commandFactory == null)
        {
            return;
        }

        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var discoveredCommands = commandDiscoveryService.DiscoverCommands(currentAssembly);
            
            AnsiConsole.MarkupLine($"[cyan]Discovered {discoveredCommands.Count} commands[/]");
            
            var registeredCount = 0;
            var skippedCount = 0;
            
            foreach (var commandInfo in discoveredCommands.OrderBy(c => c.Priority).ThenBy(c => c.Name))
            {
                try
                {
                    if (commandFactory.CanCreateCommand(commandInfo))
                    {
                        commandRegistry.RegisterCommand(commandInfo);
                        registeredCount++;
                    }
                    else
                    {
                        skippedCount++;
                        AnsiConsole.MarkupLine($"[yellow]Skipped command '{commandInfo.Name}' - dependencies not met[/]");
                    }
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    AnsiConsole.MarkupLine($"[red]Failed to register command '{commandInfo.Name}': {ex.Message}[/]");
                }
            }
            
            AnsiConsole.MarkupLine($"[green]Successfully registered {registeredCount} commands[/]");
            if (skippedCount > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipped {skippedCount} commands[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Command discovery failed: {ex.Message}[/]");
        }
    }

    private static ToolHandler CreateToolHandler(IModeManager modeManager, ApplicationSettings settings)
    {
        if (_toolRegistry == null || _toolFactory == null)
        {
            return new ToolHandler(modeManager);
        }
        
        return new ToolHandler(_toolRegistry, modeManager, _toolFactory);
    }

    private static CommandHandler CreateCommandHandler(ApplicationSettings settings, ConfigurationService configService, IModeManager modeManager)
    {
        var commandRegistry = _serviceContainer?.GetService<LogiQCLI.Commands.Core.Interfaces.ICommandRegistry>();
        var commandFactory = _serviceContainer?.GetService<LogiQCLI.Commands.Core.Interfaces.ICommandFactory>();
        
        if (commandRegistry == null || commandFactory == null)
        {
            throw new InvalidOperationException("Command system not properly initialized");
        }
        
        var coreCommandHandler = new LogiQCLI.Commands.Core.CommandHandler(commandRegistry, commandFactory);
        
        return new CommandHandler(coreCommandHandler);
    }

    private static void RenderError(string message)
    {
        var errorPanel = new Panel($"[bold red]{message}[/]")
            .Header("[red]ERROR[/]")
            .Border(BoxBorder.Heavy)
            .BorderColor(Color.Red)
            .Padding(1, 0);
        
        AnsiConsole.Write(errorPanel);
    }

    private static async Task RunTestModeAsync(string[] args)
    {
        try
        {
            InitializeServices(new ApplicationSettings { Workspace = Directory.GetCurrentDirectory() });
            
            var testRunner = new TestRunner(_serviceContainer!);
            
            var categoryFilter = GetArgumentValue(args, "-category", "--category");
            var testNameFilter = GetArgumentValue(args, "-test", "--test-name");
            
            bool testsPassed;
            
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                testsPassed = await testRunner.RunTestsByCategoryAsync(categoryFilter);
            }
            else if (!string.IsNullOrEmpty(testNameFilter))
            {
                testsPassed = await testRunner.RunSpecificTestAsync(testNameFilter);
            }
            else
            {
                testsPassed = await testRunner.RunAllTestsAsync();
            }
            
            Environment.Exit(testsPassed ? 0 : 1);
        }
        catch (Exception ex)
        {
            RenderError($"Test execution failed: {ex.Message}");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Environment.Exit(1);
        }
    }

    private static string? GetArgumentValue(string[] args, params string[] flags)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (flags.Contains(args[i]))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static void PromptForWorkspace(ApplicationSettings settings, ConfigurationService configService)
    {
        var executingDirectory = Directory.GetCurrentDirectory();
        var lastWorkspace = settings.Workspace;

        AnsiConsole.MarkupLine("[cyan]Workspace Directory Selection[/]");

        if (!string.IsNullOrWhiteSpace(lastWorkspace))
        {
            AnsiConsole.MarkupLine($"[dim]Last workspace: {lastWorkspace}[/]");
        }

        var input = AnsiConsole.Ask<string>(
            $"[green]Enter workspace path[/] [dim](press Enter for current directory: {executingDirectory})[/]:",
            string.Empty);

        if (string.IsNullOrWhiteSpace(input))
        {
            input = executingDirectory;
        }

        if (!Directory.Exists(input))
        {
            try
            {
                Directory.CreateDirectory(input);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create directory '{input}': {ex.Message}. Using current directory instead.[/]");
                input = executingDirectory;
            }
        }

        settings.Workspace = input;
        configService.SaveSettings(settings);

        try
        {
            Directory.SetCurrentDirectory(input);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to change to workspace directory '{input}': {ex.Message}[/]");
        }
    }
}
