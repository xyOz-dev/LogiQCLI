using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter;
using LogiQCLI.Core.Models.Configuration;
using Spectre.Console;

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
        private readonly ModelMetadataService _metadataService;
        private readonly ApplicationSettings _settings;

        public ModelCommand(ChatSession chatSession, ModelMetadataService metadataService, ApplicationSettings settings)
        {
            _chatSession = chatSession ?? throw new ArgumentNullException(nameof(chatSession));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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

        public override async Task<string> Execute(string args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    if (_settings.AvailableModels == null || _settings.AvailableModels.Count == 0)
                        return "[yellow]No quick models configured. Run /models to add some.[/]";

                    var prompt = new SelectionPrompt<string>()
                        .Title("[green]Select a model:[/]")
                        .AddChoices(_settings.AvailableModels)
                        .PageSize(10);

                    var chosen = AnsiConsole.Prompt(prompt);
                    if (!string.IsNullOrWhiteSpace(chosen))
                    {
                        _chatSession.Model = chosen;
                        _settings.DefaultModel = chosen;
                        return $"[green]Model set to: {chosen}[/]";
                    }

                    return string.Empty;
                }

                if (args.Trim().Equals("refresh", StringComparison.OrdinalIgnoreCase))
                {
                    var keys = _settings.ModelMetadata?.Keys.ToList() ?? new List<string>();
                    if (!keys.Any())
                    {
                        return "[yellow]No cached models to refresh.[/]";
                    }

                    var refreshed = 0;
                    foreach (var key in keys)
                    {
                        var parts = key.Split('/', 2);
                        if (parts.Length != 2) continue;
                        try
                        {
                            await _metadataService.GetModelMetadataAsync(parts[0], parts[1], true);
                            refreshed++;
                        }
                        catch { }
                    }

                    return $"[green]Refreshed metadata for {refreshed} model(s).[/]";
                }

                ModelCommandArguments? arguments = null;
                
    
                try
                {
                    arguments = JsonSerializer.Deserialize<ModelCommandArguments>(args);
                }
                catch
                {
    
                    arguments = new ModelCommandArguments { Model = args.Trim() };
                }

                if (arguments?.Model != null)
                {
                    var candidate = arguments.Model.Trim();
                    if (!candidate.Contains('/'))
                    {
                        return "[red]Model must be specified as 'provider/model' (e.g., 'openai/gpt-4o').[/]";
                    }

                    _chatSession.Model = candidate;
                    _settings.DefaultModel = candidate;

                    try
                    {
                        var parts = candidate.Split('/', 2);
                        if (parts.Length == 2)
                        {
                            await _metadataService.GetModelMetadataAsync(parts[0], parts[1]);
                        }
                    }
                    catch {  }

                    return $"[green]Model updated to: {_chatSession.Model}[/]";
                }

                return $"[yellow]Current model: {_chatSession.Model}[/]";
            }
            catch (Exception ex)
            {
                return $"[red]Error: {ex.Message}[/]";
            }
        }
    }
} 
