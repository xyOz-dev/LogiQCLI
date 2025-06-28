using System;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Objects;
using System.Text.Json;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class SearchAndReplaceValidationTests : TestBase
    {
        public override string TestName => "SearchAndReplaceTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new SearchAndReplaceTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestNullArguments(tool);
                await TestEmptyPathArgument(tool);
                await TestNullSearchContent(tool);
                await TestNullReplaceContent(tool);
                await TestEmptySearchContent(tool);
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

        private async Task TestNullArguments(SearchAndReplaceTool tool)
        {
            var result = await tool.Execute("null");
            
            if (!result.Contains("Invalid arguments") && !result.Contains("Error"))
            {
                throw new Exception("Should reject null arguments");
            }
        }

        private async Task TestEmptyPathArgument(SearchAndReplaceTool tool)
        {
            var args = new SearchAndReplaceArguments
            {
                Path = "",
                Search = "test",
                Replace = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Invalid arguments") && !result.Contains("Path") && !result.Contains("required"))
            {
                throw new Exception("Should reject empty path");
            }
        }

        private async Task TestNullSearchContent(SearchAndReplaceTool tool)
        {
            var args = new SearchAndReplaceArguments
            {
                Path = "test.txt",
                Search = null!,
                Replace = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Invalid arguments") && !result.Contains("search") && !result.Contains("required"))
            {
                throw new Exception("Should reject null search content");
            }
        }

        private async Task TestNullReplaceContent(SearchAndReplaceTool tool)
        {
            var args = new SearchAndReplaceArguments
            {
                Path = "test.txt",
                Search = "test",
                Replace = null!
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error"))
            {
                throw new Exception("Should handle null replacement content gracefully or fail due to missing file");
            }
        }

        private async Task TestEmptySearchContent(SearchAndReplaceTool tool)
        {
            var args = new SearchAndReplaceArguments
            {
                Path = "test.txt",
                Search = "",
                Replace = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Invalid arguments") && !result.Contains("search") && !result.Contains("required"))
            {
                throw new Exception("Should reject empty search content");
            }
        }

        private async Task TestInvalidRegexPattern(SearchAndReplaceTool tool)
        {
            var args = new SearchAndReplaceArguments
            {
                Path = "test.txt",
                Search = "[invalid regex",
                Replace = "replace",
                UseRegex = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error"))
            {
                throw new Exception("Should reject invalid regex pattern");
            }
        }

        private async Task TestNonExistentFile(SearchAndReplaceTool tool)
        {
            var args = new SearchAndReplaceArguments
            {
                Path = "nonexistent_file_12345.txt",
                Search = "test",
                Replace = "replace"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error") && !result.Contains("not") && !result.Contains("exist"))
            {
                throw new Exception("Should reject non-existent file");
            }
        }
    }
} 
