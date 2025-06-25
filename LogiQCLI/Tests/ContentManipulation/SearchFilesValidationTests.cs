using System;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Arguments;
using System.Text.Json;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class SearchFilesValidationTests : TestBase
    {
        public override string TestName => "SearchFilesTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new SearchFilesTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestNullArguments(tool);
                await TestEmptyPatternArgument(tool);
                await TestNullPatternArgument(tool);
                await TestInvalidRegexPattern(tool);
                await TestNonExistentDirectory(tool);
                await TestZeroMaxResults(tool);
                await TestNegativeMaxResults(tool);
                
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

        private async Task TestNullArguments(SearchFilesTool tool)
        {
            var result = await tool.Execute("null");
            
            if (!result.Contains("Invalid arguments") && !result.Contains("Error"))
            {
                throw new Exception("Should reject null arguments");
            }
        }

        private async Task TestEmptyPatternArgument(SearchFilesTool tool)
        {
            var args = new SearchFilesArguments
            {
                Pattern = "",
                Path = "."
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Invalid arguments") && !result.Contains("Pattern") && !result.Contains("required"))
            {
                throw new Exception("Should reject empty pattern");
            }
        }

        private async Task TestNullPatternArgument(SearchFilesTool tool)
        {
            var args = new SearchFilesArguments
            {
                Pattern = null,
                Path = "."
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Invalid arguments") && !result.Contains("Pattern") && !result.Contains("required"))
            {
                throw new Exception("Should reject null pattern");
            }
        }

        private async Task TestInvalidRegexPattern(SearchFilesTool tool)
        {
            var args = new SearchFilesArguments
            {
                Pattern = "[invalid regex",
                Path = ".",
                UseRegex = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error"))
            {
                throw new Exception("Should handle invalid regex pattern gracefully");
            }
        }

        private async Task TestNonExistentDirectory(SearchFilesTool tool)
        {
            var args = new SearchFilesArguments
            {
                Pattern = "test",
                Path = "nonexistent_directory_12345"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error") && !result.Contains("does not exist"))
            {
                throw new Exception("Should reject non-existent directory");
            }
        }

        private async Task TestZeroMaxResults(SearchFilesTool tool)
        {
            var args = new SearchFilesArguments
            {
                Pattern = "test",
                Path = ".",
                MaxResults = 0
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result.Contains("Error"))
            {
                throw new Exception("Zero max results should be handled gracefully");
            }
        }

        private async Task TestNegativeMaxResults(SearchFilesTool tool)
        {
            var args = new SearchFilesArguments
            {
                Pattern = "test",
                Path = ".",
                MaxResults = -5
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result.Contains("Error"))
            {
                throw new Exception("Negative max results should be handled gracefully");
            }
        }
    }
} 
