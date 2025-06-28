using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.FileOperations;
using LogiQCLI.Tools.FileOperations.Arguments;

namespace LogiQCLI.Tests.FileOperations
{
    public class CreateFileValidationTests : TestBase
    {
        public override string TestName => "CreateFileTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new CreateFileTool();
            
            try
            {
                await TestNullArguments(tool);
                await TestEmptyPath(tool);
                await TestInvalidPath(tool);
                
                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
        }

        private async Task TestNullArguments(CreateFileTool tool)
        {
            var result = await tool.Execute(string.Empty);
            
            if (!result.Contains("Error"))
            {
                throw new Exception("Should handle null arguments gracefully");
            }
        }

        private async Task TestEmptyPath(CreateFileTool tool)
        {
            var args = new CreateFileArguments
            {
                Path = "",
                Content = "test content"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error") || !result.Contains("Path is required"))
            {
                throw new Exception("Should reject empty path");
            }
        }

        private async Task TestInvalidPath(CreateFileTool tool)
        {
            var args = new CreateFileArguments
            {
                Path = "invalid|path<>?.txt",
                Content = "test content"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error creating file"))
            {
                throw new Exception("Should handle invalid path characters");
            }
        }
    }
} 
