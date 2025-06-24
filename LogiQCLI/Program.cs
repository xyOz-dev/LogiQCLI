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
using Spectre.Console;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tests.Core;
using System.Text;

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
            var settings = configService.LoadSettings();

            if (settings == null)
            {
                var configManager = new InteractiveConfigurationManager();
                settings = configManager.ConfigureInteractively();
                settings.UserDataPath = configService.GetDataDirectory();
                configService.SaveSettings(settings);
            }

            ValidateEnvironment(settings);
            Directory.SetCurrentDirectory(settings.Workspace);

            InitializeServices(settings);
            InitializeToolSystem();

            var modeManager = new ModeManager(configService, _toolRegistry);
            var openRouter = _serviceContainer.GetService<OpenRouterClient>();
            var toolHandler = CreateToolHandler(modeManager, settings);

            var chatUI = new ChatInterface(openRouter, toolHandler, settings, configService, modeManager, _toolRegistry);
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
                apiKey
            )
        );

        var gitHubClient = new GitHubClientWrapper(settings.GitHub?.Token);
        _serviceContainer.RegisterInstance(gitHubClient);
        
        _serviceContainer.RegisterInstance<IServiceContainer>(_serviceContainer);
        
        _toolRegistry = new ToolRegistry();
        _serviceContainer.RegisterInstance<IToolRegistry>(_toolRegistry);
        
        _toolFactory = new ToolFactory(_serviceContainer);
        _serviceContainer.RegisterInstance<IToolFactory>(_toolFactory);
        
        _toolDiscoveryService = new ToolDiscoveryService();
        _serviceContainer.RegisterInstance<IToolDiscoveryService>(_toolDiscoveryService);
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
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Tool discovery failed: {ex.Message}[/]");
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
            
            var testRunner = new TestRunner(_serviceContainer);
            
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

    private static string GetArgumentValue(string[] args, params string[] flags)
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
}
