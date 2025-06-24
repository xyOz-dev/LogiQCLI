using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Tools.Core;
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
            var toolResults = new List<Message>();
            
            RenderToolExecutionHeader(toolCalls.Count, usage);
            
            foreach (var toolCall in toolCalls)
            {
                var result = await ExecuteSingleToolAsync(toolCall);
                toolResults.Add(result);
            }

            RenderToolExecutionComplete();
            return toolResults;
        }

        private async Task<Message> ExecuteSingleToolAsync(ToolCall toolCall)
        {
            var startTime = DateTime.Now;
            string result = string.Empty;
            bool hasError = false;
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync($"[yellow]Executing {toolCall.Function.Name}...[/]", async ctx =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(toolCall.Function.Arguments))
                        {
                            result = "Error: No arguments provided for tool call";
                            hasError = true;
                            return;
                        }
                        
                        result = await _toolHandler.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments);
                        
                        if (string.IsNullOrEmpty(result))
                        {
                            result = "Tool executed successfully with no output";
                        }
                        
                        var duration = (DateTime.Now - startTime).TotalMilliseconds;
                        ctx.Status($"[green]✓[/] {toolCall.Function.Name} completed in {duration:F0}ms");
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        result = $"Error: Invalid JSON arguments - {jsonEx.Message}";
                        hasError = true;
                        ctx.Status($"[red]✗[/] {toolCall.Function.Name} failed - Invalid JSON");
                    }
                    catch (Exception ex)
                    {
                        result = $"Error executing tool: {ex.Message}";
                        hasError = true;
                        ctx.Status($"[red]✗[/] {toolCall.Function.Name} failed");
                    }
                    
                    await Task.Delay(500);
                });

            RenderToolResult(toolCall.Function.Name, result, hasError);
            
            return new Message
            {
                Role = "tool",
                ToolCallId = toolCall.Id,
                Name = toolCall.Function.Name,
                Content = result
            };
        }

        private void RenderToolExecutionHeader(int toolCount, Usage? usage)
        {
            AnsiConsole.WriteLine();
            var headerText = "[yellow]⚡ Tool Execution[/]";
            if (usage != null)
            {
                headerText += $" [grey](Cost: {usage.Cost:C3})[/]";
            }
            
            var panel = new Panel($"[yellow]Executing {toolCount} tool{(toolCount > 1 ? "s" : "")}[/]")
                .Header(headerText)
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex("#ffaf00"))
                .Padding(1, 0)
                .Expand();
            
            AnsiConsole.Write(panel);
        }

        private void RenderToolResult(string toolName, string result, bool hasError = false)
        {
            var displayResult = TruncateResult(result);
            var borderColor = hasError ? Color.FromHex("#ff5f5f") : Color.FromHex("#00ff87");
            var headerColor = hasError ? "[red]" : "[green]";
            
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(borderColor)
                .AddColumn($"{headerColor}Tool[/]")
                .AddColumn($"{headerColor}Result[/]")
                .AddRow(
                    $"[bold]{toolName}[/]",
                    hasError
                        ? $"[red]{Markup.Escape(displayResult)}[/]"
                        : $"[dim]{Markup.Escape(displayResult)}[/]"
                );
            
            AnsiConsole.Write(table);
        }

        private void RenderToolExecutionComplete()
        {
            var rule = new Rule("[green]✓ Tools Execution Complete[/]")
                .RuleStyle("green");
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        private string TruncateResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "No output";

            const int maxLength = 100;
            if (result.Length <= maxLength)
                return result;

            return result.Substring(0, maxLength) + "...";
        }
    }
}
