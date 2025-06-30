using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Infrastructure.ApiClients.LMStudio.Objects;
using LogiQCLI.Infrastructure.Providers;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Core.Services;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Commands.Session
{
    [CommandMetadata("Session", Tags = new[] { "experimental" })]
    public class CompressCommand : ICommand
    {
        private readonly ChatSession _chatSession;
        private readonly IServiceContainer _container;
        private readonly ApplicationSettings _settings;
        private readonly Action _initializeDisplay;

        public CompressCommand(ChatSession chatSession, IServiceContainer container, ApplicationSettings settings, Action initializeDisplay)
        {
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _initializeDisplay = initializeDisplay ?? throw new ArgumentNullException(nameof(initializeDisplay));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "compress",
                Description = "Compress the current chat history into a concise summary while preserving the first and last three messages."            };
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
            var firstMessage = allMessages.FirstOrDefault(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));

            var lastThree = allMessages
                .Reverse()
                .Take(3)
                .Reverse()
                .ToArray();

            var messagesToCompress = allMessages
                .Except(new[] { systemMessage, firstMessage }.Where(m => m != null)!)
                .Except(lastThree)
                .ToArray();

            if (messagesToCompress.Length == 0)
            {
                return "[yellow]Nothing to compress (history too short).[/]";
            }

            var sb = new StringBuilder();
            foreach (var msg in messagesToCompress)
            {
                var role = msg.Role ?? "unknown";
                var content = msg.Content?.ToString() ?? string.Empty;
                sb.AppendLine($"{role.ToUpper()}: {content}");
            }

            var compressionPrompt = "You are a summarization assistant. Given the following chat transcript, generate a concise summary that preserves all essential facts, decisions, code references, and instructions. The summary should be short but complete enough that no important context is lost.";

            var request = new ChatRequest
            {
                Model = _chatSession.Model,
                Messages = new[]
                {
                    new Message { Role = "system", Content = compressionPrompt },
                    new Message { Role = "user", Content = sb.ToString() }
                },
                Usage = new UsageRequest()
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

            var summaryMessage = new Message { Role = "assistant", Content = summaryContent, Name = "compress" };

            
            _chatSession.ClearHistory();

            if (firstMessage != null)
            {
                _chatSession.AddMessage(firstMessage);
            }

            _chatSession.AddMessage(summaryMessage);

            foreach (var msg in lastThree)
            {
                if (msg != firstMessage)
                {
                    _chatSession.AddMessage(msg);
                }
            }

            _initializeDisplay();

            return "[bold green]Chat compressed successfully.[/]";
        }
    }
} 