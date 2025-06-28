using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.FileOperations;
using LogiQCLI.Tools.FileOperations.Arguments;

namespace LogiQCLI.Tests.FileOperations
{
    public class AppendFileValidationTests : TestBase
    {
        public override string TestName => "AppendFileTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new AppendFileTool();

            try
            {
                await TestNullArguments(tool);
                await TestEmptyPath(tool);
                await TestInvalidPath(tool);
                await TestNullPath(tool);

                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
        }

        private async Task TestNullArguments(AppendFileTool tool)
        {
            var result = await tool.Execute(string.Empty);
            if (!result.Contains("Error"))
            {
                throw new Exception("Should handle null arguments string gracefully");
            }
        }

        private async Task TestEmptyPath(AppendFileTool tool)
        {
            var args = new AppendFileArguments
            {
                Path = string.Empty,
                Content = "content"
            };
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            if (!result.Contains("Error") || !result.Contains("Path is required"))
            {
                throw new Exception("Should reject empty path");
            }
        }

        private async Task TestInvalidPath(AppendFileTool tool)
        {
            var args = new AppendFileArguments
            {
                Path = "invalid|path<>?.txt",
                Content = "content"
            };
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            if (!result.Contains("Error appending to file"))
            {
                throw new Exception("Should handle invalid path characters");
            }
        }

        private async Task TestNullPath(AppendFileTool tool)
        {
            var args = new AppendFileArguments
            {
                Content = "content"
            };
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            if (!result.Contains("Error") || !result.Contains("Path is required"))
            {
                throw new Exception("Should reject null path value");
            }
        }
    }
} 