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
        private readonly string _modelName;

        public MessageRenderer(string modelName = "ASSISTANT")
        {
            _messageHistory = new List<Panel>();
            _renderLock = new object();
            _modelName = modelName;
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

                    AnsiConsole.Write(panel);
                    AnsiConsole.WriteLine();
                }
            });
        }

        private Panel CreateMessagePanel(Message message, MessageStyle style, Usage? usage = null, decimal totalCost = 0)
        {
            var (color, header, border) = GetStyleConfiguration(style);
            var content = FormatMessageContent((string)(message.Content ?? string.Empty));
            
            if (style == MessageStyle.Assistant && usage != null)
            {
                var requestCost = usage.Cost.ToString("C3");
                var totalSessionCost = totalCost.ToString("C3");
                var totalTokens = usage.PromptTokens + usage.CompletionTokens - (usage.PromptTokensDetails?.CachedTokens ?? 0);
                
                header = $"Request Cost: {requestCost} | Total Cost: {totalSessionCost} | Prompt: {usage.PromptTokens} | Completion: {usage.CompletionTokens} | Total: {totalTokens}";

                if (usage.PromptTokensDetails?.CachedTokens > 0)
                {
                    header += $" | Cached: {usage.PromptTokensDetails.CachedTokens}";
                }
            }

            var panel = new Panel(content)
                .Header($"[{color}]{header}[/]")
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
                MessageStyle.Assistant => ("#5f87ff", _modelName.ToUpper(), BoxBorder.Rounded),
                MessageStyle.System => ("#808080", "SYSTEM", BoxBorder.Square),
                MessageStyle.Tool => ("#ffaf00", "TOOL", BoxBorder.Heavy),
                _ => ("#ffffff", "MESSAGE", BoxBorder.Rounded)
            };
        }

        private string FormatMessageContent(string content)
        {
            // Trim whitespace and normalize line endings for better formatting
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
    }
}
