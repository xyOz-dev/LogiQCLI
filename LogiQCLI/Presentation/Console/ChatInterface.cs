using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Presentation.Console.Components;
using LogiQCLI.Presentation.Console.Animation;
using LogiQCLI.Presentation.Console.Session;
using Spectre.Console;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using LogiQCLI.Presentation.Console.Components.Objects;
using LogiQCLI.Core.Models.Modes.Interfaces;
using LogiQCLI.Tools.Core.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

namespace LogiQCLI.Presentation.Console
{
    public class ChatInterface
    {
        private readonly OpenRouterClient _openRouterClient;
        private readonly ToolHandler _toolHandler;
        private readonly ApplicationSettings _settings;
        private readonly IModeManager _modeManager;
        private readonly IToolRegistry _toolRegistry;
        private readonly MessageRenderer _messageRenderer;
        private readonly AnimationManager _animationManager;
        private readonly ChatSession _chatSession;
        private readonly FileReadRegistry _fileReadRegistry;
        private readonly InputHandler _inputHandler;
        private readonly HeaderRenderer _headerRenderer;
        private readonly CommandHandler _commandHandler;
        private decimal _totalCost = 0;

        public ChatInterface(
            OpenRouterClient openRouterClient,
            ToolHandler toolHandler,
            ApplicationSettings settings,
            ConfigurationService configService,
            IModeManager modeManager,
            IToolRegistry toolRegistry,
            CommandHandler commandHandler,
            ChatSession chatSession,
            FileReadRegistry fileReadRegistry)
        {
            _openRouterClient = openRouterClient;
            _toolHandler = toolHandler;
            _settings = settings;
            _modeManager = modeManager;
            _toolRegistry = toolRegistry;
            _messageRenderer = new MessageRenderer(_settings.DefaultModel ?? "ASSISTANT");
            _animationManager = new AnimationManager();
            _chatSession = chatSession;
            _fileReadRegistry = fileReadRegistry;
            _inputHandler = new InputHandler();
            _headerRenderer = new HeaderRenderer(_settings, _modeManager);
            _commandHandler = commandHandler;
        }

        public async Task RunAsync()
        {
            InitializeDisplay();

            while (true)
            {
                var userInput = await GetUserInputAsync();
                if (string.IsNullOrWhiteSpace(userInput)) continue;

                if (_commandHandler.IsCommand(userInput))
                {
                    await _commandHandler.ExecuteCommand(userInput);
                }
                else
                {
                    await ProcessUserMessageAsync(userInput);
                    await GenerateAssistantResponseAsync();
                }
            }
        }

        private void InitializeDisplay()
        {
            AnsiConsole.Clear();
            _headerRenderer.RenderHeader();
            _headerRenderer.RenderWelcomeMessage();
            _messageRenderer.RenderChatArea();
        }

        private async Task<string> GetUserInputAsync()
        {
            return await _inputHandler.GetInputAsync();
        }

        private async Task ProcessUserMessageAsync(string message)
        {
            var userMessage = new Message { Role = "user", Content = message };
            _chatSession.AddMessage(userMessage);
        }

        private async Task GenerateAssistantResponseAsync()
        {
            var activeApiKey = _settings.GetActiveApiKey();
            if (activeApiKey == null || string.IsNullOrEmpty(activeApiKey.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]No API key configured. Use /settings to add an API key.[/]");
                return;
            }

            var request = CreateChatRequest();
            ChatResponse? response = null;
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("#5f87ff"))
                .StartAsync("[#5f87ff]Thinking...[/]", async ctx =>
                {
                    try
                    {
                        response = await _openRouterClient.Chat(request);
                        if (response?.Usage != null)
                        {
                            _totalCost += (decimal)response.Usage.Cost;
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error calling API: {ex.Message}[/]");
                    }
                });

            if (response?.Choices == null || response.Choices.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]No response received from the API[/]");
                return;
            }

            var choice = response.Choices[0];
            var message = choice.Message;

            if (message == null)
            {
                AnsiConsole.MarkupLine("[red]Invalid response format[/]");
                return;
            }

            if (message.ToolCalls != null && message.ToolCalls.Length > 0)
            {
                _chatSession.AddMessage(message);
                await ProcessToolCallsAsync(message.ToolCalls.ToList(), response.Usage);
                await GenerateAssistantResponseAsync();
            }
            else if (message.Content != null)
            {
                _chatSession.AddMessage(message);
                await _messageRenderer.RenderMessageAsync(message, MessageStyle.Assistant, response.Usage, _totalCost);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Empty response from assistant[/]");
            }
        }


        private ChatRequest CreateChatRequest()
        {
            return new ChatRequest
            {
                Model = _chatSession.Model,
                Messages = _chatSession.GetMessages(),
                Tools = _toolHandler.GetToolDefinitions(),
                ToolChoice = "auto",
                Usage = new UsageRequest()
            };
        }

        private async Task ProcessToolCallsAsync(List<ToolCall> toolCalls, Usage? usage)
        {
            var toolExecutor = new ToolExecutor(_toolHandler);
            var toolResults = await toolExecutor.ExecuteToolsAsync(toolCalls, usage);

            for (int i = 0; i < toolResults.Count; i++)
            {
                var result = toolResults[i];
                var call = toolCalls[i];

                if (IsFileReadCall(call))
                {
                    if (result.Content?.ToString() == "__UNCHANGED__")
                    {
                        AnsiConsole.MarkupLine($"[grey]Skipped unchanged read of {ExtractPathFromArguments(call.Function.Arguments)} (kept prior content)[/]");
                        result.Content = "__UNCHANGED__";
                        _chatSession.AddMessage(result);
                        continue;
                    }

                    if (_settings.Experimental == null || !_settings.Experimental.DeduplicateFileReads)
                    {
                        _chatSession.AddMessage(result);
                        continue;
                    }

                    var path = ExtractPathFromArguments(call.Function.Arguments);
                    if (string.IsNullOrEmpty(path))
                    {
                        _chatSession.AddMessage(result);
                        continue;
                    }

                    var fullPath = System.IO.Path.GetFullPath(path.Replace('/', System.IO.Path.DirectorySeparatorChar).Replace('\\', System.IO.Path.DirectorySeparatorChar));

                    var hash = ComputeHash(result.Content?.ToString() ?? string.Empty);

                    if (_fileReadRegistry.TryGet(fullPath, out var entry))
                    {
                        if (entry.Hash == hash)
                        {
                            AnsiConsole.MarkupLine($"[grey]Skipped unchanged read of {fullPath} (kept prior content)[/]");
                            result.Content = "__UNCHANGED__";
                            _chatSession.AddMessage(result);
                            continue;
                        }

                        _chatSession.RemoveMessage(entry.MessageRef);
                        _fileReadRegistry.Remove(fullPath);
                        AnsiConsole.MarkupLine($"[yellow]Replaced previous read of {fullPath}[/]");
                    }

                    _chatSession.AddMessage(result);

                    var info = new System.IO.FileInfo(fullPath);
                    _fileReadRegistry.Register(fullPath, hash, info.LastWriteTimeUtc, info.Length, result);
                }
                else
                {
                    _chatSession.AddMessage(result);
                }
            }
        }

        private static bool IsFileReadCall(ToolCall call)
        {
            var name = call.Function.Name?.ToLowerInvariant();
            return name == "read_file" || name == "read_file_by_line_count";
        }

        private static string ExtractPathFromArguments(string argumentsJson)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(argumentsJson);
                if (dict != null && dict.TryGetValue("path", out var pathObj))
                {
                    return pathObj?.ToString() ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }

        private static string ComputeHash(string content)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hashBytes = sha.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
