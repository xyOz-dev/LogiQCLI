using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Presentation.Console.Components.Objects;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class ToolExecutor
    {
        private readonly ToolHandler _toolHandler;

        public ToolExecutor(ToolHandler toolHandler)
        {
            _toolHandler = toolHandler;
        }

        public async Task<List<Message>> ExecuteToolsAsync(List<ToolCall> toolCalls, Usage? usage = null)
        {
            if (toolCalls == null) throw new ArgumentNullException(nameof(toolCalls));

            var toolCount = toolCalls.Count;
            RenderToolExecutionHeader(toolCount, usage);

            if (toolCount == 1)
            {
                var singleResult = await ExecuteSingleToolAsync(toolCalls[0], true);
                RenderToolExecutionComplete();
                return new List<Message> { singleResult };
            }

            var toolResults = new Message[toolCount];

            var executionTasks = toolCalls.Select((toolCall, index) => Task.Run(async () =>
            {
                var result = await ExecuteSingleToolAsync(toolCall, false);
                toolResults[index] = result;
            })).ToList();

            await Task.WhenAll(executionTasks);

            RenderToolExecutionComplete();

            return toolResults.ToList();
        }

        private static readonly object _consoleLock = new object();

        private async Task<Message> ExecuteSingleToolAsync(ToolCall toolCall, bool interactive)
        {
            var startTime = DateTime.Now;
            string result = string.Empty;
            bool hasError = false;

            if (toolCall.Function == null)
            {
                return new Message
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Name = "unknown",
                    Content = "Error: Tool call function is null"
                };
            }

            if (toolCall.Function.Name?.ToLowerInvariant() == "execute_command" && !string.IsNullOrEmpty(toolCall.Function.Arguments))
            {
                RenderPreExecutionCommand(toolCall.Function.Arguments);
            }

            if (interactive)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .StartAsync($"[yellow]Executing {toolCall.Function.Name ?? "unknown"}...[/]", async ctx =>
                    {
                        await ExecuteToolLogic();
                        var duration = (DateTime.Now - startTime).TotalMilliseconds;
                        ctx.Status($"[green]✓[/] {toolCall.Function.Name ?? "unknown"} completed in {duration:F0}ms");
                    });
            }
            else
            {
                await ExecuteToolLogic();
            }

            lock (_consoleLock)
            {
                RenderEnhancedToolResult(toolCall.Function.Name ?? "unknown", toolCall.Function.Arguments ?? string.Empty, result, hasError);
            }

            return new Message
            {
                Role = "tool",
                ToolCallId = toolCall.Id,
                Name = toolCall.Function.Name ?? "unknown",
                Content = result
            };

            async Task ExecuteToolLogic()
            {
                try
                {
                    if (string.IsNullOrEmpty(toolCall.Function.Arguments))
                    {
                        result = "Error: No arguments provided for tool call";
                        hasError = true;
                        return;
                    }

                    result = await _toolHandler.ExecuteTool(toolCall.Function.Name ?? string.Empty, toolCall.Function.Arguments);

                    if (string.IsNullOrEmpty(result))
                    {
                        result = "Tool executed successfully with no output";
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    result = $"Error: Invalid JSON arguments - {jsonEx.Message}";
                    hasError = true;
                }
                catch (Exception ex)
                {
                    result = $"Error executing tool: {ex.Message}";
                    hasError = true;
                }
                finally
                {
                    await Task.Delay(500);
                }
            }
        }

        private void RenderToolExecutionHeader(int toolCount, Usage? usage)
        {
            AnsiConsole.WriteLine();
            var headerText = "[yellow]⚡ Tool Execution[/]";
            if (usage != null)
            {
                headerText += $" [grey](Cost: {usage.Cost.ToString("C3", CultureInfo.GetCultureInfo("en-US"))})[/]";
            }
            
            var panel = new Panel($"[yellow]Executing {toolCount} tool{(toolCount > 1 ? "s" : "")}[/]")
                .Header(headerText)
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex("#ffaf00"))
                .Padding(1, 0)
                .Expand();
            
            AnsiConsole.Write(Align.Center(panel));
        }

        private void RenderEnhancedToolResult(string toolName, string arguments, string result, bool hasError = false)
        {
            ToolDisplayFormatter.RenderEnhancedToolResult(toolName, arguments, result, hasError);
        }

        private void RenderToolExecutionComplete()
        {
            var rule = new Rule("[green]✓ Tools Execution Complete[/]")
                .RuleStyle("green");
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        private void RenderPreExecutionCommand(string arguments)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var command = args?.GetValueOrDefault("command")?.ToString();
                var workingDir = args?.GetValueOrDefault("cwd")?.ToString();
                var sessionId = args?.GetValueOrDefault("session_id")?.ToString();

                if (string.IsNullOrEmpty(command)) return;

                AnsiConsole.WriteLine();
                
                var commandInfo = new List<string>();
                commandInfo.Add($"[cyan]$ {Markup.Escape(command)}[/]");
                
                if (!string.IsNullOrEmpty(workingDir))
                {
                    commandInfo.Add($"[dim]Working Directory: {Markup.Escape(workingDir)}[/]");
                }
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    commandInfo.Add($"[dim]Session: {Markup.Escape(sessionId)}[/]");
                }

                var panel = new Panel(string.Join("\n", commandInfo))
                    .Header("[blue]💻 Executing Command[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Blue)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
            }
            catch
            {
            }
        }


    }
}
