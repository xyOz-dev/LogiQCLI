using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiQCLI.Core.Models.Modes
{
    public class Mode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public List<string> AllowedTools { get; set; } = new List<string>();
        public string? PreferredModel { get; set; }
        public bool IsBuiltIn { get; set; } = false;
        
        public List<string> AllowedCategories { get; set; } = new List<string>();
        public List<string> ExcludedCategories { get; set; } = new List<string>();
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> ExcludedTags { get; set; } = new List<string>();
        
        public bool IsToolAllowed(string toolName, string? category = null, List<string>? tags = null)
        {
            if (AllowedTools.Any(t => string.Equals(t, toolName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            
            if (AllowedTools.Any() && !AllowedCategories.Any() && !AllowedTags.Any())
            {
                return false;
            }
            
            if (category != null)
            {
                if (ExcludedCategories.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                
                if (AllowedCategories.Any() && !AllowedCategories.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }
            
            if (tags != null && tags.Any())
            {
                if (ExcludedTags.Any(t => tags.Any(tag => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))))
                {
                    return false;
                }
                
                if (AllowedTags.Any() && !AllowedTags.Any(t => tags.Any(tag => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}