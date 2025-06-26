using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiQCLI.Core.Models.Modes
{
    public class ModeBuilder
    {
        private readonly Mode _mode;

        public ModeBuilder()
        {
            _mode = new Mode();
        }

        public ModeBuilder(Mode existingMode)
        {
            _mode = existingMode ?? throw new ArgumentNullException(nameof(existingMode));
        }

        public ModeBuilder WithId(string id)
        {
            _mode.Id = id;
            return this;
        }

        public ModeBuilder WithName(string name)
        {
            _mode.Name = name;
            return this;
        }

        public ModeBuilder WithDescription(string description)
        {
            _mode.Description = description;
            return this;
        }

        public ModeBuilder WithSystemPrompt(string systemPrompt)
        {
            _mode.SystemPrompt = systemPrompt;
            return this;
        }

        public ModeBuilder WithPreferredModel(string model)
        {
            _mode.PreferredModel = model;
            return this;
        }

        public ModeBuilder AsBuiltIn(bool isBuiltIn = true)
        {
            _mode.IsBuiltIn = isBuiltIn;
            return this;
        }

        public ModeBuilder AllowTool(string toolName)
        {
            if (!_mode.AllowedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase))
            {
                _mode.AllowedTools.Add(toolName);
            }
            return this;
        }

        public ModeBuilder AllowTools(params string[] toolNames)
        {
            foreach (var toolName in toolNames)
            {
                AllowTool(toolName);
            }
            return this;
        }

        public ModeBuilder AllowCategory(string category)
        {
            if (!_mode.AllowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                _mode.AllowedCategories.Add(category);
            }
            return this;
        }

        public ModeBuilder AllowCategories(params string[] categories)
        {
            foreach (var category in categories)
            {
                AllowCategory(category);
            }
            return this;
        }

        public ModeBuilder ExcludeCategory(string category)
        {
            if (!_mode.ExcludedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                _mode.ExcludedCategories.Add(category);
            }
            return this;
        }

        public ModeBuilder ExcludeCategories(params string[] categories)
        {
            foreach (var category in categories)
            {
                ExcludeCategory(category);
            }
            return this;
        }

        public ModeBuilder AllowTag(string tag)
        {
            if (!_mode.AllowedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                _mode.AllowedTags.Add(tag);
            }
            return this;
        }

        public ModeBuilder AllowTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                AllowTag(tag);
            }
            return this;
        }

        public ModeBuilder ExcludeTag(string tag)
        {
            if (!_mode.ExcludedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                _mode.ExcludedTags.Add(tag);
            }
            return this;
        }

        public ModeBuilder ExcludeTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                ExcludeTag(tag);
            }
            return this;
        }

        public ModeBuilder AllowAllTools()
        {
            _mode.AllowedTools.Clear();
            _mode.AllowedCategories.Clear();
            _mode.ExcludedCategories.Clear();
            _mode.AllowedTags.Clear();
            _mode.ExcludedTags.Clear();
            return this;
        }

        public Mode Build()
        {
            return _mode;
        }

        public static implicit operator Mode(ModeBuilder builder)
        {
            return builder.Build();
        }
    }
}
