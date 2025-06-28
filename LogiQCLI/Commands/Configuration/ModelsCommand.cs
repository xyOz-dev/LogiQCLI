using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Presentation.Console.Animation;
using LogiQCLI.Presentation.Console.Components.Configuration;
using Spectre.Console;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Configuration
{
    public class ModelsCommandArguments
    {
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
    }

    [CommandMetadata("Configuration", Tags = new[] { "models" })]
    public class ModelsCommand : ICommand
    {
        private readonly ApplicationSettings _settings;
        private readonly IModelDiscoveryService _discoveryService;

        public ModelsCommand(ApplicationSettings settings, IModelDiscoveryService discoveryService)
        {
            _settings = settings;
            _discoveryService = discoveryService;
        }

        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "models",
                Description = "Manage available model list",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        action = new { type = "string", description = "list | add | remove | refresh | pick" },
                        model = new { type = "string", description = "model id for add/remove" }
                    }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            ModelsCommandArguments? parsed = null;
            if (!string.IsNullOrWhiteSpace(args))
            {
                try { parsed = JsonSerializer.Deserialize<ModelsCommandArguments>(args); }
                catch { parsed = new ModelsCommandArguments { Action = args.Trim() }; }
            }
            parsed ??= new ModelsCommandArguments();
            var action = (parsed.Action ?? "pick").ToLowerInvariant();
            switch (action)
            {
                case "list":
                    return string.Join("\n", _settings.AvailableModels);
                case "add":
                    if (string.IsNullOrWhiteSpace(parsed.Model)) return "[red]Model id required[/]";
                    if (!_settings.AvailableModels.Contains(parsed.Model)) _settings.AvailableModels.Add(parsed.Model);
                    return "[green]Model added[/]";
                case "remove":
                    if (string.IsNullOrWhiteSpace(parsed.Model)) return "[red]Model id required[/]";
                    _settings.AvailableModels.Remove(parsed.Model);
                    if (_settings.DefaultModel == parsed.Model) _settings.DefaultModel = _settings.AvailableModels.Count > 0 ? _settings.AvailableModels[0] : null;
                    return "[green]Model removed[/]";
                case "refresh":
                    var refreshed = await _discoveryService.GetAllModelIdsAsync(_settings, true);
                    _settings.AvailableModels = new List<string>(refreshed);
                    return "[green]Models refreshed[/]";
                case "pick":
                default:
                    var picker = new ModelPicker(new AnimationManager(), _discoveryService, _settings);
                    picker.Run();
                    return "[green]Models updated[/]";
            }
        }
    }
} 