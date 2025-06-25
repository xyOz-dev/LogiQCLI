using System;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Objects;
using System.Text.Json;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class ApplyDiffValidationTests : TestBase
    {
        public override string TestName => "ApplyDiffTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new ApplyDiffTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestNullArguments(tool);
                await TestEmptyPathArgument(tool);
                await TestNullOriginalContent(tool);
                await TestNullReplacementContent(tool);
                await TestEmptyOriginalContent(tool);
                await TestZeroMaxReplacements(tool);
                await TestInvalidRegexPattern(tool);
                await TestNonExistentFile(tool);
                
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

        private async Task TestNullArguments(ApplyDiffTool tool)
        {
            var result = await tool.Execute("null");
            
            if (!result.Contains("Invalid arguments") && !result.Contains("Error"))
            {
                throw new Exception("Should reject null arguments");
            }
        }

        private async Task TestEmptyPathArgument(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "",
                Original = "test",
                Replacement = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Path is required"))
            {
                throw new Exception("Should reject empty path");
            }
        }

        private async Task TestNullOriginalContent(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "test.txt",
                Original = null,
                Replacement = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Original content cannot be null"))
            {
                throw new Exception("Should reject null original content");
            }
        }

        private async Task TestNullReplacementContent(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "test.txt",
                Original = "test",
                Replacement = null
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Replacement content cannot be null"))
            {
                throw new Exception("Should reject null replacement content");
            }
        }

        private async Task TestEmptyOriginalContent(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "test.txt",
                Original = "",
                Replacement = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Original content cannot be empty"))
            {
                throw new Exception("Should reject empty original content");
            }
        }

        private async Task TestZeroMaxReplacements(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "test.txt",
                Original = "test",
                Replacement = "replace",
                MaxReplacements = 0
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("MaxReplacements cannot be 0"))
            {
                throw new Exception("Should reject zero MaxReplacements");
            }
        }

        private async Task TestInvalidRegexPattern(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "test.txt",
                Original = "[invalid regex",
                Replacement = "replace",
                UseRegex = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Invalid regex pattern"))
            {
                throw new Exception("Should reject invalid regex pattern");
            }
        }

        private async Task TestNonExistentFile(ApplyDiffTool tool)
        {
            var args = new ApplyDiffArguments
            {
                Path = "nonexistent_file_12345.txt",
                Original = "test",
                Replacement = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("File not found"))
            {
                throw new Exception("Should reject non-existent file");
            }
        }
    }
}
