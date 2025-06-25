using System;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class MarkdownParser
    {
        private static readonly Regex BoldRegex = new(@"\*\*(.*?)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled);
        private static readonly Regex StrikethroughRegex = new(@"~~(.*?)~~", RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex HeaderRegex = new(@"^(#{1,6})\s+(.+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex UnorderedListRegex = new(@"^[\s]*[-*+]\s+(.+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex OrderedListRegex = new(@"^[\s]*\d+\.\s+(.+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex BlockquoteRegex = new(@"^>\s*(.+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex CodeBlockRegex = new(@"```(\w+)?\n?(.*?)\n?```", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string ParseMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "[dim]No content[/]";

            var result = new StringBuilder();
            var lines = markdown.Split('\n');
            bool inCodeBlock = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (line.Trim().StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    if (inCodeBlock)
                    {
                        var language = line.Trim().Substring(3).Trim();
                        if (!string.IsNullOrEmpty(language))
                        {
                            result.AppendLine($"[dim]┌─ {Markup.Escape(language)} ─[/]");
                        }
                        else
                        {
                            result.AppendLine("[dim]┌─ code ─[/]");
                        }
                    }
                    else
                    {
                        result.AppendLine("[dim]└─────[/]");
                    }
                }
                else if (inCodeBlock)
                {
                    result.AppendLine($"[grey85 on grey15] {Markup.Escape(line)} [/]");
                }
                else
                {
                    var processedLine = ParseInlineMarkdown(line);
                    // Only add non-empty processed lines, or preserve intentional empty lines
                    if (!string.IsNullOrEmpty(processedLine) || string.IsNullOrEmpty(line.Trim()))
                    {
                        result.AppendLine(processedLine);
                    }
                }
            }

            return result.ToString().TrimEnd();
        }

        private static string ParseInlineMarkdown(string line)
        {
            if (string.IsNullOrEmpty(line))
                return "";

            var processed = Markup.Escape(line);

            processed = HeaderRegex.Replace(processed, match =>
            {
                var level = match.Groups[1].Value.Length;
                var text = match.Groups[2].Value;
                return level switch
                {
                    1 => $"[bold yellow]{text}[/]",
                    2 => $"[bold cyan]{text}[/]",
                    3 => $"[bold green]{text}[/]",
                    4 => $"[bold blue]{text}[/]",
                    5 => $"[bold magenta]{text}[/]",
                    6 => $"[bold red]{text}[/]",
                    _ => $"[bold]{text}[/]"
                };
            });

            processed = UnorderedListRegex.Replace(processed, match =>
            {
                var text = match.Groups[1].Value;
                return $"[cyan]•[/] {text}";
            });

            processed = OrderedListRegex.Replace(processed, match =>
            {
                var text = match.Groups[1].Value;
                return $"[cyan]▸[/] {text}";
            });

            processed = BlockquoteRegex.Replace(processed, match =>
            {
                var text = match.Groups[1].Value;
                return $"[dim]│[/] [italic]{text}[/]";
            });

            processed = BoldRegex.Replace(processed, "[bold yellow]$1[/]");
            
            processed = ItalicRegex.Replace(processed, "[italic cyan]$1[/]");
            
            processed = StrikethroughRegex.Replace(processed, "[strikethrough dim]$1[/]");
            
            processed = InlineCodeRegex.Replace(processed, "[grey85 on grey15] $1 [/]");
            
            processed = LinkRegex.Replace(processed, "[underline blue]$1[/] [dim]($2)[/]");

            return processed;
        }
    }
}