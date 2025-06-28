using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Presentation.Console.Components.Objects;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class MessageRenderer
    {
        private readonly List<Panel> _messageHistory;
        private readonly object _renderLock;
        private string _modelName;

        public MessageRenderer(string modelName = "ASSISTANT")
        {
            _messageHistory = new List<Panel>();
            _renderLock = new object();
            _modelName = modelName;
        }

        public void SetModelName(string modelName)
        {
            if (!string.IsNullOrWhiteSpace(modelName))
            {
                _modelName = modelName.Trim();
            }
        }

        public void RenderChatArea()
        {
            var rule = new Rule("[dim]Chat Session[/]")
                .RuleStyle("grey");
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        public async Task RenderMessageAsync(Message message, MessageStyle style, Usage? usage = null, decimal totalCost = 0)
        {
            await Task.Run(() =>
            {
                lock (_renderLock)
                {
                    var panel = CreateMessagePanel(message, style, usage, totalCost);
                    _messageHistory.Add(panel);

                    AnsiConsole.Write(Align.Center(panel));
                    AnsiConsole.WriteLine();
                }
            });
        }

        private Panel CreateMessagePanel(Message message, MessageStyle style, Usage? usage = null, decimal totalCost = 0)
        {
            var (color, header, border) = GetStyleConfiguration(style);
            var content = FormatMessageContent((string)(message.Content ?? string.Empty));
            
            var panel = new Panel(content)
                .Header($"[{color}]{header}[/]")
                .HeaderAlignment(style == MessageStyle.Assistant ? Justify.Center : Justify.Left)
                .Border(border)
                .BorderColor(Color.FromHex(color))
                .Padding(1, 0, 1, 0)
                .Expand();

            return panel;
        }

        private (string color, string header, BoxBorder border) GetStyleConfiguration(MessageStyle style)
        {
            return style switch
            {
                MessageStyle.User => ("#00ff87", "YOU", BoxBorder.Rounded),
                MessageStyle.Assistant => ("#5f87ff", _modelName, BoxBorder.Rounded),
                MessageStyle.System => ("#808080", "SYSTEM", BoxBorder.Square),
                MessageStyle.Tool => ("#ffaf00", "TOOL", BoxBorder.Heavy),
                _ => ("#ffffff", "MESSAGE", BoxBorder.Rounded)
            };
        }

        private string FormatMessageContent(string content)
        {
            var normalizedContent = content?.Trim() ?? string.Empty;
            return MarkdownParser.ParseMarkdown(normalizedContent);
        }

        public void ClearHistory()
        {
            lock (_renderLock)
            {
                _messageHistory.Clear();
                AnsiConsole.Clear();
            }
        }

        public void RenderUsagePanel(Usage usage, decimal totalCost, int contextUsed, int contextLength)
        {
            var percentUsed = contextLength > 0 ? (double)contextUsed / contextLength : 0;
            var barWidth = Math.Min(System.Console.WindowWidth - 20, 60);
            if (barWidth < 10) barWidth = 10;
            var filled = (int)Math.Round(percentUsed * barWidth);
            var barColor = percentUsed < 0.5 ? "#00ff87" : percentUsed < 0.8 ? "#ffaf00" : "#ff0000";
            var bar = $"[{barColor}]{new string('█', filled)}[/]{new string('░', barWidth - filled)}";

            var pieces = new List<string>
            {
                $"Prompt {usage.PromptTokens}",
                $"Completion {usage.CompletionTokens}"
            };

            if (usage.PromptTokensDetails?.CachedTokens > 0)
            {
                pieces.Add($"Cached {usage.PromptTokensDetails.CachedTokens}");
            }

            pieces.Add($"Total {usage.PromptTokens + usage.CompletionTokens}");
            pieces.Add($"Cost {usage.Cost:C3}");
            pieces.Add($"Session {totalCost:C3}");

            var info = string.Join(" | ", pieces);

            var body = $"{info}\n{bar} [grey]({contextUsed}/{contextLength})[/]";

            var panel = new Panel(body)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex(barColor))
                .Padding(1,0,1,0);

            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.WriteLine();
        }
    }
}
