using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components.Objects
{
    public static class ToolDisplayFormatter
    {
        private const int MaxDisplayLines = 70;
        private const int MaxLineLength = 500;

        public static void RenderEnhancedToolResult(string toolName, string arguments, string result, bool hasError = false)
        {
            if (hasError)
            {
                RenderBasicToolResult(toolName, result, hasError);
                return;
            }

            switch (toolName.ToLowerInvariant())
            {
                case "read_file":
                    RenderFileReadResult(arguments, result);
                    break;
                case "read_file_by_line_count":
                    RenderFileReadByLineCountResult(arguments, result);
                    break;
                case "apply_diff":
                    RenderApplyDiffResult(arguments, result);
                    break;
                case "write_file":
                    RenderWriteFileResult(arguments, result);
                    break;
                case "create_file":
                    RenderCreateFileResult(arguments, result);
                    break;
                case "append_file":
                    RenderAppendFileResult(arguments, result);
                    break;
                case "delete_file":
                    RenderDeleteFileResult(arguments, result);
                    break;
                case "move_file":
                    RenderMoveFileResult(arguments, result);
                    break;
                case "execute_command":
                    RenderExecuteCommandResult(arguments, result);
                    break;
                default:
                    RenderBasicToolResult(toolName, result, hasError);
                    break;
            }
        }

        private static void RenderFileReadResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                
                var lines = result.Split('\n');
                var totalLines = lines.Length;
                var fileSize = System.Text.Encoding.UTF8.GetByteCount(result);
                
                var panel = new Panel((Spectre.Console.Rendering.IRenderable)CreateFileContentDisplay(result, lines))
                    .Header($"[green]📖 File Read: {Markup.Escape(filePath)}[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Padding(1, 0)
                    .Expand();

                var infoText = $"[dim]Lines: {totalLines} | Size: {FormatFileSize(fileSize)}[/]";
                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine(infoText);
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("read_file", result, false);
            }
        }

        private static void RenderFileReadByLineCountResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                var lineCount = args?.GetValueOrDefault("lineCount")?.ToString() ?? "Unknown";
                
                var lines = result.Split('\n');
                var actualLines = lines.Length;
                
                var panel = new Panel((Spectre.Console.Rendering.IRenderable)CreateFileContentDisplay(result, lines))
                    .Header($"[green]📖 File Read (First {lineCount} lines): {Markup.Escape(filePath)}[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Padding(1, 0)
                    .Expand();

                var infoText = $"[dim]Requested: {lineCount} lines | Actual: {actualLines} lines[/]";
                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine(infoText);
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("read_file_by_line_count", result, false);
            }
        }

        private static void RenderApplyDiffResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                var original = args?.GetValueOrDefault("original")?.ToString() ?? "";
                var replacement = args?.GetValueOrDefault("replacement")?.ToString() ?? "";
                var isPreview = args?.GetValueOrDefault("preview")?.ToString()?.ToLowerInvariant() == "true";
                
                if (result.StartsWith("Error:"))
                {
                    RenderBasicToolResult("apply_diff", result, true);
                    return;
                }

                var diffDisplay = CreateDiffDisplay(original, replacement, isPreview);
                var headerText = isPreview 
                    ? $"[yellow]🔍 Diff Preview: {Markup.Escape(filePath)}[/]"
                    : $"[green]✏️ Applied Diff: {Markup.Escape(filePath)}[/]";

                var panel = new Panel((Spectre.Console.Rendering.IRenderable)diffDisplay)
                    .Header(headerText)
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(isPreview ? Color.Yellow : Color.Green)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                var statusColor = isPreview ? "yellow" : "green";
                AnsiConsole.MarkupLine($"[{statusColor}]{Markup.Escape(result)}[/]");
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("apply_diff", result, false);
            }
        }

        private static void RenderWriteFileResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                var content = args?.GetValueOrDefault("content")?.ToString() ?? "";
                
                if (result.StartsWith("Error:"))
                {
                    RenderBasicToolResult("write_file", result, true);
                    return;
                }

                var contentPreview = CreateContentPreview(content);
                var panel = new Panel((Spectre.Console.Rendering.IRenderable)contentPreview)
                    .Header($"[blue]💾 File Written: {Markup.Escape(filePath)}[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Blue)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(result)}[/]");
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("write_file", result, false);
            }
        }

        private static void RenderCreateFileResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                var content = args?.GetValueOrDefault("content")?.ToString() ?? "";
                var overwrite = args?.GetValueOrDefault("overwrite")?.ToString()?.ToLowerInvariant() == "true";
                
                if (result.StartsWith("Error:"))
                {
                    RenderBasicToolResult("create_file", result, true);
                    return;
                }

                var contentPreview = CreateContentPreview(content);
                var icon = overwrite ? "🔄" : "📄";
                var panel = new Panel((Spectre.Console.Rendering.IRenderable)contentPreview)
                    .Header($"[cyan]{icon} File Created: {Markup.Escape(filePath)}[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.FromHex("#00ffff"))
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(result)}[/]");
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("create_file", result, false);
            }
        }

        private static void RenderAppendFileResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                var content = args?.GetValueOrDefault("content")?.ToString() ?? "";
                
                if (result.StartsWith("Error:"))
                {
                    RenderBasicToolResult("append_file", result, true);
                    return;
                }

                var contentPreview = CreateContentPreview(content);
                var panel = new Panel((Spectre.Console.Rendering.IRenderable)contentPreview)
                    .Header($"[yellow]📝 Content Appended: {Markup.Escape(filePath)}[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(result)}[/]");
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("append_file", result, false);
            }
        }

        private static void RenderDeleteFileResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var filePath = args?.GetValueOrDefault("path")?.ToString() ?? "Unknown";
                
                var hasError = result.StartsWith("Error:");
                var color = hasError ? Color.Red : Color.Orange3;
                var headerColor = hasError ? "red" : "orange3";
                var icon = hasError ? "❌" : "🗑️";

                var panel = new Panel(new Markup($"[dim]File path: {Markup.Escape(filePath)}[/]"))
                    .Header($"[{headerColor}]{icon} File Deletion[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(color)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                var resultColor = hasError ? "red" : "green";
                AnsiConsole.MarkupLine($"[{resultColor}]{Markup.Escape(result)}[/]");
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("delete_file", result, false);
            }
        }

        private static void RenderMoveFileResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var sourcePath = args?.GetValueOrDefault("source")?.ToString() ?? "Unknown";
                var destinationPath = args?.GetValueOrDefault("destination")?.ToString() ?? "Unknown";
                
                var hasError = result.StartsWith("Error:");
                var color = hasError ? Color.Red : Color.Purple;
                var headerColor = hasError ? "red" : "purple";
                var icon = hasError ? "❌" : "📁";

                var moveInfo = new Markup($"[dim]From: {Markup.Escape(sourcePath)}[/]\n[dim]To: {Markup.Escape(destinationPath)}[/]");
                var panel = new Panel(moveInfo)
                    .Header($"[{headerColor}]{icon} File Move[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(color)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                var resultColor = hasError ? "red" : "green";
                AnsiConsole.MarkupLine($"[{resultColor}]{Markup.Escape(result)}[/]");
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("move_file", result, false);
            }
        }

        private static void RenderExecuteCommandResult(string arguments, string result)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                var command = args?.GetValueOrDefault("command")?.ToString() ?? "Unknown command";
                var workingDir = args?.GetValueOrDefault("cwd")?.ToString();
                var sessionId = args?.GetValueOrDefault("session_id")?.ToString();
                var timeout = args?.GetValueOrDefault("timeout")?.ToString();
                
                if (result.StartsWith("Error:"))
                {
                    RenderBasicToolResult("execute_command", result, true);
                    return;
                }

                // Extract session ID from result if present
                var sessionIdFromResult = string.Empty;
                if (!string.IsNullOrEmpty(sessionId) && result.StartsWith("Session ID: "))
                {
                    var lines = result.Split('\n');
                    if (lines.Length > 0)
                    {
                        sessionIdFromResult = lines[0].Replace("Session ID: ", "").Trim();
                        result = string.Join('\n', lines.Skip(1));
                    }
                }
                var outputLines = result.Split('\n');
                var truncated = outputLines.Length > MaxDisplayLines;
                var displayLines = outputLines.Take(MaxDisplayLines).ToArray();
                
                var outputDisplay = string.Join("\n", displayLines.Select(line => 
                {
                    var trimmedLine = line.TrimEnd();
                    return trimmedLine.Length > MaxLineLength 
                        ? Markup.Escape(trimmedLine.Substring(0, MaxLineLength - 3)) + "..."
                        : Markup.Escape(trimmedLine);
                }));

                if (truncated)
                {
                    outputDisplay += $"\n[dim]... and {outputLines.Length - MaxDisplayLines} more lines (output truncated)[/]";
                }

                var outputContent = string.IsNullOrWhiteSpace(result) 
                    ? "[dim]<no output>[/]" 
                    : outputDisplay;

                var panel = new Panel(new Markup(outputContent))
                    .Header("[green]📤 Command Output[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Padding(1, 0)
                    .Expand();

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();
            }
            catch
            {
                RenderBasicToolResult("execute_command", result, false);
            }
        }

        private static void RenderBasicToolResult(string toolName, string result, bool hasError = false)
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
            
            AnsiConsole.Write(Align.Center(table));
        }

        private static object CreateFileContentDisplay(string content, string[] lines)
        {
            var displayLines = lines.Take(MaxDisplayLines).ToArray();
            var truncated = lines.Length > MaxDisplayLines;

            var sb = new System.Text.StringBuilder();
            var consoleWidth = System.Console.WindowWidth > 0 ? System.Console.WindowWidth : MaxLineLength;
            var contentWidth = consoleWidth - 10;
            if (contentWidth < 20) contentWidth = 20;

            for (int i = 0; i < displayLines.Length; i++)
            {
                var lineNumber = (i + 1).ToString().PadLeft(4);
                var rawLine = displayLines[i];
                var displayLine = rawLine.Length <= contentWidth ? rawLine : rawLine.Substring(0, contentWidth - 3) + "...";

                sb.Append("[dim]").Append(lineNumber).Append("[/] ")
                  .Append(Markup.Escape(displayLine));

                if (i < displayLines.Length - 1 || truncated)
                {
                    sb.Append('\n');
                }
            }

            if (truncated)
            {
                var remainingLines = lines.Length - MaxDisplayLines;
                sb.Append("[dim]... and ").Append(remainingLines).Append(" more lines[/]");
            }

            return new Markup(sb.ToString());
        }

        private static object CreateContentPreview(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return new Markup("[dim]<empty file>[/]");
            }

            var lines = content.Split('\n');
            var totalLines = lines.Length;
            var fileSize = System.Text.Encoding.UTF8.GetByteCount(content);
            
            var displayLines = lines.Take(10).ToArray();
            var truncated = lines.Length > 10;
            
            var contentLines = displayLines.Select((line, index) => 
            {
                var lineNumber = (index + 1).ToString().PadLeft(3);
                var displayLine = TruncateLine(line);
                return new Markup($"[dim]{lineNumber}[/] {Markup.Escape(displayLine)}");
            }).ToList();

            if (truncated)
            {
                var remainingLines = lines.Length - 10;
                contentLines.Add(new Markup($"[dim]... and {remainingLines} more lines[/]"));
            }

            contentLines.Add(new Markup($"[dim]Total: {totalLines} lines, {FormatFileSize(fileSize)}[/]"));
            
            return new Rows(contentLines.ToArray());
        }

        private static object CreateDiffDisplay(string original, string replacement, bool isPreview)
        {
            var originalLines = original.Split('\n');
            var replacementLines = replacement.Split('\n');
            
            var diffLines = new List<object>();
            
            diffLines.Add(new Markup("[dim]@@ Diff View @@[/]"));
            if (!string.IsNullOrEmpty(original))
            {
                foreach (var line in originalLines.Take(15))
                {
                    var displayLine = TruncateLine(line);
                    var wrappedLines = WrapLine(displayLine, MaxLineLength - 4);
                    foreach (var wrappedLine in wrappedLines)
                    {
                        diffLines.Add(new Markup($"[red]- {Markup.Escape(wrappedLine)}[/]"));
                    }
                }
                if (originalLines.Length > 15)
                {
                    diffLines.Add(new Markup($"[dim red]... and {originalLines.Length - 15} more lines[/]"));
                }
            }
            
            if (!string.IsNullOrEmpty(replacement))
            {
                foreach (var line in replacementLines.Take(15))
                {
                    var displayLine = TruncateLine(line);
                    var wrappedLines = WrapLine(displayLine, MaxLineLength - 4);
                    foreach (var wrappedLine in wrappedLines)
                    {
                        diffLines.Add(new Markup($"[green]+ {Markup.Escape(wrappedLine)}[/]"));
                    }
                }
                if (replacementLines.Length > 15)
                {
                    diffLines.Add(new Markup($"[dim green]... and {replacementLines.Length - 15} more lines[/]"));
                }
            }
            
            return new Rows(diffLines.Cast<Spectre.Console.Rendering.IRenderable>().ToArray());
        }

        private static string TruncateLine(string line)
        {
            if (line.Length <= MaxLineLength)
                return line;
            
            return line.Substring(0, MaxLineLength - 3) + "...";
        }

        private static List<string> WrapLine(string line, int maxWidth)
        {
            var wrapped = new List<string>();
            
            if (line.Length <= maxWidth)
            {
                wrapped.Add(line);
                return wrapped;
            }
            

            var words = line.Split(' ');
            var currentLine = "";
            
            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(currentLine))
                {
                    currentLine = word;
                }
                else if (currentLine.Length + word.Length + 1 <= maxWidth)
                {
                    currentLine += " " + word;
                }
                else
                {
                    wrapped.Add(currentLine);
                    currentLine = word;
                }
            }
            
            if (!string.IsNullOrEmpty(currentLine))
            {
                wrapped.Add(currentLine);
            }
            
            return wrapped;
        }

        private static string TruncateResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "No output";

            const int maxLength = 100;
            if (result.Length <= maxLength)
                return result;

            return result.Substring(0, maxLength) + "...";
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
