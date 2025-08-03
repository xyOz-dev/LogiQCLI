using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.Providers;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Tools.Core.Interfaces;
using Spectre.Console;

namespace LogiQCLI.Commands.Session
{
    [CommandMetadata("Session", Tags = new[] { "experimental" }, Alias = "compact")]
    public class CompressCommand : ICommand
    {
        private const int MaxTranscriptChars = 100_000;

        private readonly ChatSession _chatSession;
        private readonly IServiceContainer _container;
        private readonly ApplicationSettings _settings;
        private readonly Action _initializeDisplay;
        private readonly FileReadRegistry _fileReadRegistry;

        public CompressCommand(ChatSession chatSession, IServiceContainer container, ApplicationSettings settings, Action initializeDisplay, FileReadRegistry fileReadRegistry)
        {
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
            _fileReadRegistry = fileReadRegistry ?? throw new ArgumentNullException(nameof(fileReadRegistry));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "compress",
                Alias = "compact",
                Description = "Compress the current chat history into a concise summary while preserving the first and last three messages. Maintains file read state."
            };
        }

        public override async Task<string> Execute(string args)
        {
            var activeApiKey = _settings.GetActiveApiKey();
            if (activeApiKey == null || string.IsNullOrEmpty(activeApiKey.ApiKey))
            {
                return "[red]No API key configured. Use /settings to add an API key.[/]";
            }

            var allMessages = _chatSession.GetMessages();
            if (allMessages.Length == 0)
            {
                return "[yellow]No messages to compress.[/]";
            }

            var systemMessage = allMessages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
            var nonSystemMessages = allMessages.Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (nonSystemMessages.Length <= 4)
            {
                return "[yellow]Nothing to compress (history too short - need more than 4 non-system messages).[/]";
            }

            var firstMessage = nonSystemMessages.First();
            var lastThree = nonSystemMessages.TakeLast(3).ToArray();
            var messagesToCompress = nonSystemMessages.Skip(1).SkipLast(3).ToArray();
            if (messagesToCompress.Length == 0)
            {
                return "[yellow]Nothing to compress (history too short).[/]";
            }

            var originalTokenCount = EstimateTokenCount(allMessages);

            var sb = new StringBuilder();
            foreach (var msg in messagesToCompress)
            {
                var role = msg.Role ?? "unknown";
                var content = msg.Content?.ToString() ?? string.Empty;

                if (msg.ToolCalls?.Any() == true)
                {
                    sb.AppendLine($"{role.ToUpper()}: {content}");
                    sb.AppendLine($"[Tool calls made: {string.Join(", ", msg.ToolCalls.Select(tc => tc.Function?.Name ?? "unknown"))}]");
                }
                else
                {
                    sb.AppendLine($"{role.ToUpper()}: {content}");
                }

                if (sb.Length > MaxTranscriptChars)
                {
                    var head = MaxTranscriptChars - 8000;
                    if (head < 0) head = MaxTranscriptChars;
                    var tail = 8000;
                    var prefix = sb.ToString(0, Math.Min(head, sb.Length));
                    var suffixStart = Math.Max(0, sb.Length - tail);
                    var suffix = sb.ToString(suffixStart, sb.Length - suffixStart);
                    sb.Clear();
                    sb.Append(prefix);
                    sb.AppendLine();
                    sb.AppendLine("[... truncated for compression ...]");
                    sb.Append(suffix);
                    break;
                }
            }

            var compressionPrompt = "You are a summarization assistant. Given the following chat transcript, generate a concise summary that preserves all essential facts, decisions, code references, file operations, tool usage, and instructions. Pay special attention to any file reads, code changes, or tool operations. The summary should be short but complete enough that no important context is lost for continued development work.";

            var usage = new UsageRequest();
            if (_settings?.Inference != null)
            {
                usage.MaxCompletionTokens = _settings.Inference.MaxCompletionTokens;
            }

            var request = new ChatRequest
            {
                Model = _chatSession.Model,
                Messages = new[]
                {
                    new Message { Role = "system", Content = compressionPrompt },
                    new Message { Role = "user", Content = sb.ToString() }
                },
                Usage = usage
            };

            var provider = ProviderFactory.Create(_container);
            object? responseObj;
            try
            {
                responseObj = await provider.CreateChatCompletionAsync(request);
            }
            catch (Exception ex)
            {
                return $"[red]Compression failed: {ex.Message}[/]";
            }

            ChatResponse? response = responseObj as ChatResponse;
            if (response == null && responseObj is LMStudioChatResponse lmResponse)
            {
                response = new ChatResponse
                {
                    Id = lmResponse.Id,
                    Choices = lmResponse.Choices?.Select(c => new Choice
                    {
                        Message = new Message
                        {
                            Role = c.Message?.Role ?? "assistant",
                            Content = c.Message?.Content ?? string.Empty
                        }
                    }).ToArray()
                };
            }

            if (response == null || response.Choices == null || response.Choices.Length == 0)
            {
                return "[red]No summary was returned by the model.[/]";
            }

            var summaryContent = response.Choices[0].Message?.Content?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(summaryContent))
            {
                return "[red]Received an empty summary from the model.[/]";
            }

            var preservedMessages = new[] { firstMessage }.Concat(lastThree).ToArray();
            var fileReadEntriesToPreserve = new Dictionary<string, (string Hash, DateTime LastWriteUtc, long Length, Message MessageRef)>();

            foreach (var message in preservedMessages)
            {
                if (IsFileReadResult(message))
                {
                    var path = ExtractFilePathFromMessage(message);
                    if (!string.IsNullOrEmpty(path) && _fileReadRegistry.TryGet(path, out var entry))
                    {
                        fileReadEntriesToPreserve[path] = entry;
                    }
                }
            }

            var summaryMessage = new Message { Role = "assistant", Content = "[COMPRESSED SUMMARY]\n" + summaryContent };

            _chatSession.ClearHistory();

            if (systemMessage != null)
            {
                _chatSession.AddMessage(systemMessage);
            }

            _chatSession.AddMessage(firstMessage);
            _chatSession.AddMessage(summaryMessage);

            foreach (var msg in lastThree)
            {
                _chatSession.AddMessage(msg);
            }

            foreach (var (path, entry) in fileReadEntriesToPreserve)
            {
                var preservedMessage = preservedMessages.FirstOrDefault(m => m == entry.MessageRef);
                if (preservedMessage != null)
                {
                    _fileReadRegistry.Register(path, entry.Hash, entry.LastWriteUtc, entry.Length, preservedMessage);
                }
            }

            var newMessages = _chatSession.GetMessages();
            var newTokenCount = EstimateTokenCount(newMessages);
            var tokensSaved = originalTokenCount - newTokenCount;
            var compressionRatio = originalTokenCount > 0 ? (double)tokensSaved / originalTokenCount : 0;

            int contextLength = 0;
            try
            {
                var metadataService = _container.GetService<ModelMetadataService>();
                if (metadataService != null)
                {
                    var parts = _chatSession.Model.Split('/', 2);
                    ModelEndpointsData? meta = null;
                    if (parts.Length == 2)
                    {
                        meta = await metadataService.GetModelMetadataAsync(parts[0], parts[1]);
                    }
                    else if (parts.Length == 1)
                    {
                        meta = await metadataService.GetModelMetadataAsync(_settings.DefaultProvider ?? "", parts[0]);
                    }

                    var best = metadataService.GetBestEndpoint(meta);
                    if (best != null)
                    {
                        contextLength = best.ContextLength;
                    }
                }
            }
            catch {  }

            DisplayCompressionStats(allMessages.Length, messagesToCompress.Length, newMessages.Length,
                                  originalTokenCount, newTokenCount, tokensSaved, compressionRatio,
                                  fileReadEntriesToPreserve.Count, response?.Usage,
                                  newTokenCount, contextLength);

            return string.Empty;
        }

        private void DisplayCompressionStats(int originalMessages, int compressedMessages, int finalMessages,
                                           int originalTokens, int newTokens, int tokensSaved, double compressionRatio,
                                           int preservedFileReads, Usage? compressionUsage,
                                           int contextUsed = 0, int contextLength = 0)
        {
            var compressionPercent = (compressionRatio * 100).ToString("F1");
            var compressionColor = compressionRatio > 0.5 ? "#00ff87" : compressionRatio > 0.3 ? "#ffaf00" : "#ff8700";

            var pieces = new List<string>
            {
                $"Messages {originalMessages}->{finalMessages}",
                $"Tokens {originalTokens}->{newTokens} (-{tokensSaved})",
                $"Compression Rate {compressionPercent}%"
            };

            if (preservedFileReads > 0)
            {
                pieces.Add($"Reads {preservedFileReads}");
            }

            if (compressionUsage != null)
            {
                pieces.Add($"Cost {compressionUsage.Cost.ToString("C4", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}");
            }

            if (contextLength > 0)
            {
                pieces.Add($"Context {contextUsed}/{contextLength}");
            }

            var infoLine = string.Join(" | ", pieces);

            var progressBar = CreateCompressionProgressBar(compressionRatio);
            var content = $"{infoLine}\n{progressBar}";

            var panel = new Panel(content)
                .Header("[bold green]📦 Compression[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex(compressionColor))
                .Padding(1, 0, 1, 0)
                .Expand();

            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.WriteLine();
        }

        private string CreateCompressionProgressBar(double compressionRatio)
        {
            var barWidth = Math.Min(Console.WindowWidth - 30, 50);
            if (barWidth < 10) barWidth = 10;

            var filled = (int)Math.Round(compressionRatio * barWidth);
            var compressionColor = compressionRatio > 0.5 ? "#00ff87" : compressionRatio > 0.3 ? "#ffaf00" : "#ff8700";

            var bar = $"[{compressionColor}]{new string('█', filled)}[/]{new string('░', barWidth - filled)}";
            var percentage = (compressionRatio * 100).ToString("F1");

            return $"[dim]Compression Efficiency[/]\n{bar} [bold]{percentage}%[/]";
        }

        private int EstimateTokenCount(Message[] messages)
        {
            return messages.Sum(EstimateTokenCount);
        }

        private int EstimateTokenCount(Message message)
        {
            var content = message.Content?.ToString() ?? string.Empty;
            var role = message.Role ?? string.Empty;

            var baseTokens = EstimateTokensFromText(content + role);

            if (message.ToolCalls?.Any() == true)
            {
                var toolCallContent = string.Join(" ", message.ToolCalls.Select(tc =>
                    $"{tc.Function?.Name ?? ""} {tc.Function?.Arguments ?? ""}"));
                baseTokens += EstimateTokensFromText(toolCallContent);
            }

            return baseTokens;
        }

        private int EstimateTokensFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return (int)Math.Ceiling(words.Length * 1.3);
        }

        private static bool IsFileReadResult(Message message)
        {
            return message.Role?.Equals("tool", StringComparison.OrdinalIgnoreCase) == true &&
                   message.ToolCallId != null;
        }

        private static string ExtractFilePathFromMessage(Message message)
        {
            var content = message.Content?.ToString() ?? string.Empty;

            if (content.StartsWith("File content", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("```") && content.Contains("File:"))
            {
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("File:") || line.Contains("Path:"))
                    {
                        var parts = line.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            return parts[1].Trim();
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}