using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Core.Models.Modes;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core;

namespace LogiQCLI.Tests.Core
{
    public class ModeMigrationTests : TestBase
    {
        public override string TestName => "Mode Migration Tests";

        public override async Task<TestResult> ExecuteAsync()
        {
            await Task.CompletedTask;
            var testFileSystem = new TestFileSystem();
            
            try
            {
                TestBuiltInModesExist();
                TestBuiltInModesHaveCorrectProperties();
                TestModeComparisonLogic();
                TestModeFilteringLogic();
                
                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
            finally
            {
                testFileSystem.Dispose();
            }
        }

        private void TestBuiltInModesExist()
        {

            var expectedModes = new[] { "default", "researcher", "analyst", "architect", "github_manager", "tester" };
            var builtInModes = BuiltInModes.GetBuiltInModes();
            
            if (builtInModes.Count != 6)
            {
                throw new Exception($"Expected 6 built-in modes, got {builtInModes.Count}");
            }
            
            foreach (var expectedModeId in expectedModes)
            {
                var mode = builtInModes.FirstOrDefault(m => m.Id == expectedModeId);
                if (mode == null)
                {
                    throw new Exception($"Expected built-in mode '{expectedModeId}' not found");
                }
                
                if (!mode.IsBuiltIn)
                {
                    throw new Exception($"Mode '{expectedModeId}' should be marked as built-in");
                }
            }
        }

        private void TestBuiltInModesHaveCorrectProperties()
        {

            var modes = BuiltInModes.GetBuiltInModes();
            
            foreach (var mode in modes)
            {
                if (!mode.IsBuiltIn)
                {
                    throw new Exception($"Mode {mode.Id} should be marked as built-in");
                }
                
                if (string.IsNullOrEmpty(mode.Name))
                {
                    throw new Exception($"Mode {mode.Id} has empty name");
                }
                
                if (string.IsNullOrEmpty(mode.Description))
                {
                    throw new Exception($"Mode {mode.Id} has empty description");
                }
                
                if (string.IsNullOrEmpty(mode.SystemPrompt))
                {
                    throw new Exception($"Mode {mode.Id} has empty system prompt");
                }
                

                bool hasFiltering = mode.AllowedTools.Any() || 
                                   mode.AllowedCategories.Any() || 
                                   mode.AllowedTags.Any() ||
                                   mode.ExcludedCategories.Any() || 
                                   mode.ExcludedTags.Any();
                
                if (!hasFiltering)
                {
                    throw new Exception($"Mode {mode.Id} has no tool filtering configured");
                }
            }
        }

        private void TestModeComparisonLogic()
        {

            var originalMode = new Mode
            {
                Id = "test_mode",
                Name = "Test Mode",
                Description = "Original description",
                IsBuiltIn = true,
                SystemPrompt = "Original prompt",
                AllowedTools = new List<string> { "tool1" },
                AllowedCategories = new List<string>(),
                AllowedTags = new List<string>(),
                ExcludedCategories = new List<string>(),
                ExcludedTags = new List<string>()
            };
            
            var identicalMode = new Mode
            {
                Id = "test_mode",
                Name = "Test Mode",
                Description = "Original description",
                IsBuiltIn = true,
                SystemPrompt = "Original prompt",
                AllowedTools = new List<string> { "tool1" },
                AllowedCategories = new List<string>(),
                AllowedTags = new List<string>(),
                ExcludedCategories = new List<string>(),
                ExcludedTags = new List<string>()
            };
            
            var modifiedMode = new Mode
            {
                Id = "test_mode",
                Name = "Test Mode",
                Description = "Modified description",
                IsBuiltIn = true,
                SystemPrompt = "Original prompt",
                AllowedTools = new List<string> { "tool1" },
                AllowedCategories = new List<string>(),
                AllowedTags = new List<string>(),
                ExcludedCategories = new List<string>(),
                ExcludedTags = new List<string>()
            };
            

            if (ShouldUpdateMode(originalMode, identicalMode))
            {
                throw new Exception("Identical modes should not require update");
            }
            
            if (!ShouldUpdateMode(originalMode, modifiedMode))
            {
                throw new Exception("Modified modes should require update");
            }
        }

        private void TestModeFilteringLogic()
        {

            var testTools = new List<ToolTypeInfo>
            {
                new ToolTypeInfo { Name = "read_file", Category = "FileOperations", Tags = new List<string> { "essential", "safe" } },
                new ToolTypeInfo { Name = "write_file", Category = "FileOperations", Tags = new List<string> { "write" } },
                new ToolTypeInfo { Name = "delete_file", Category = "FileOperations", Tags = new List<string> { "destructive" } },
                new ToolTypeInfo { Name = "get_issue", Category = "GitHub", Tags = new List<string> { "safe", "query" } },
                new ToolTypeInfo { Name = "execute_command", Category = "SystemOperations", Tags = new List<string> { "system" } }
            };
            
            var builtInModes = BuiltInModes.GetBuiltInModes();
            

            var defaultMode = builtInModes.First(m => m.Id == "default");
            var allowedForDefault = FilterToolsForMode(defaultMode, testTools);
            if (!allowedForDefault.Any(t => t.Name == "read_file"))
            {
                throw new Exception("Default mode should allow read_file");
            }
            

            var githubManagerMode = builtInModes.First(m => m.Id == "github_manager");
            var allowedForGitHub = FilterToolsForMode(githubManagerMode, testTools);
            if (!allowedForGitHub.Any(t => t.Name == "get_issue"))
            {
                throw new Exception("GitHub Manager mode should allow GitHub tools");
            }
            if (allowedForGitHub.Any(t => t.Name == "delete_file"))
            {
                throw new Exception("GitHub Manager mode should exclude destructive tools");
            }
            

            var analystMode = builtInModes.First(m => m.Id == "analyst");
            var allowedForAnalyst = FilterToolsForMode(analystMode, testTools);
            if (allowedForAnalyst.Any(t => t.Category == "GitHub"))
            {
                throw new Exception("Analyst mode should exclude GitHub category");
            }
        }

        private bool ShouldUpdateMode(Mode existing, Mode current)
        {

            return existing.Name != current.Name ||
                   existing.Description != current.Description ||
                   existing.SystemPrompt != current.SystemPrompt ||
                   !existing.AllowedTools.SequenceEqual(current.AllowedTools) ||
                   !existing.AllowedCategories.SequenceEqual(current.AllowedCategories) ||
                   !existing.AllowedTags.SequenceEqual(current.AllowedTags) ||
                   !existing.ExcludedCategories.SequenceEqual(current.ExcludedCategories) ||
                   !existing.ExcludedTags.SequenceEqual(current.ExcludedTags);
        }

        private List<ToolTypeInfo> FilterToolsForMode(Mode mode, List<ToolTypeInfo> tools)
        {

            var filtered = tools.ToList();
            

            if (mode.AllowedTools.Any())
            {
                filtered = filtered.Where(t => mode.AllowedTools.Contains(t.Name)).ToList();
            }
            if (mode.AllowedCategories.Any())
            {
                filtered = filtered.Where(t => mode.AllowedCategories.Contains(t.Category)).ToList();
            }
            if (mode.AllowedTags.Any())
            {
                filtered = filtered.Where(t => t.Tags.Any(tag => mode.AllowedTags.Contains(tag))).ToList();
            }
            

            if (mode.ExcludedCategories.Any())
            {
                filtered = filtered.Where(t => !mode.ExcludedCategories.Contains(t.Category)).ToList();
            }
            if (mode.ExcludedTags.Any())
            {
                filtered = filtered.Where(t => !t.Tags.Any(tag => mode.ExcludedTags.Contains(tag))).ToList();
            }
            
            return filtered;
        }
    }
} 
