using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using LogiQCLI.Core.Models.Configuration;
using LogiQCLI.Core.Services;
using LogiQCLI.Presentation.Console.Animation;

namespace LogiQCLI.Presentation.Console.Components.Configuration
{
    internal class ModelPicker
    {
        private readonly AnimationManager _animationManager;
        private readonly IModelDiscoveryService _discoveryService;
        private readonly ApplicationSettings _settings;
        private readonly bool _changeDefault;

        public ModelPicker(AnimationManager animationManager, IModelDiscoveryService discoveryService, ApplicationSettings settings, bool changeDefault = false)
        {
            _animationManager = animationManager;
            _discoveryService = discoveryService;
            _settings = settings;
            _changeDefault = changeDefault;
        }

        public void Run()
        {
            List<string> allModels = new List<string>();
            _animationManager.ShowSimpleSpinner("Fetching available models", () =>
            {
                allModels = _discoveryService.GetAllModelIdsAsync(_settings).GetAwaiter().GetResult().ToList();
            });
            if (!allModels.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No models discovered – falling back to default list.[/]");
                allModels = _settings.AvailableModels.ToList();
            }
            allModels = allModels.OrderBy(m => m).ToList();
            var preSelected = new HashSet<string>(_settings.AvailableModels, StringComparer.OrdinalIgnoreCase);
            var selectionPrompt = new MultiSelectionPrompt<string>()
                .Title("[green]Select models to include in quick list (press space to toggle, enter to accept):[/]")
                .NotRequired()
                .PageSize(15)
                .MoreChoicesText("[grey](Move up and down to reveal more models)[/]")
                .InstructionsText("[grey](Press <space> to toggle a model, <enter> to confirm)[/]");
            selectionPrompt.AddChoices(allModels);
            foreach (var model in allModels)
            {
                if (preSelected.Contains(model)) selectionPrompt.Select(model);
            }
            var selected = AnsiConsole.Prompt(selectionPrompt);
            if (!selected.Any()) selected.Add(_settings.DefaultModel ?? allModels.First());
            _settings.AvailableModels = selected.ToList();
            if (_changeDefault)
            {
                var defaultChoices = selected.ToList();
                if (!string.IsNullOrWhiteSpace(_settings.DefaultModel) && defaultChoices.Remove(_settings.DefaultModel)) defaultChoices.Insert(0, _settings.DefaultModel);
                var newDefault = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Choose default model:[/]")
                        .AddChoices(defaultChoices)
                        .PageSize(10));
                _settings.DefaultModel = newDefault;
            }
        }
    }
} 