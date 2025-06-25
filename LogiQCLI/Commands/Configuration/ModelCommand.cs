using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Configuration
{
    public class ModelCommandArguments
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    [CommandMetadata("Configuration", Tags = new[] { "settings", "model" })]
    public class ModelCommand : ICommand
    {
        private readonly ChatSession _chatSession;

        public ModelCommand(ChatSession chatSession)
        {
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "model",
                Description = "View or change the current AI model",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        model = new
                        {
                            type = "string",
                            description = "New model name (optional - omit to view current model)"
                        }
                    }
                }
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    return Task.FromResult($"[yellow]Current model: {_chatSession.Model}[/]");
                }

                ModelCommandArguments? arguments = null;
                
                // Try to parse as JSON first
                try
                {
                    arguments = JsonSerializer.Deserialize<ModelCommandArguments>(args);
                }
                catch
                {
                    // If JSON parsing fails, treat the entire args as the model name
                    arguments = new ModelCommandArguments { Model = args.Trim() };
                }

                if (arguments?.Model != null)
                {
                    _chatSession.Model = arguments.Model.Trim();
                    return Task.FromResult($"[green]Model updated to: {_chatSession.Model}[/]");
                }

                return Task.FromResult($"[yellow]Current model: {_chatSession.Model}[/]");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error: {ex.Message}[/]");
            }
        }
    }
} 