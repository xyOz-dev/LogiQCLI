using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.FileOperations;
using LogiQCLI.Tools.FileOperations.Arguments;

namespace LogiQCLI.Tests.FileOperations
{
    public class WriteFileValidationTests : TestBase
    {
        public override string TestName => "WriteFileTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new WriteFileTool();
            
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

        private async Task TestNullArguments(WriteFileTool tool)
        {
            var result = await tool.Execute(null);
            
            if (!result.Contains("Error"))
            {
                throw new Exception("Should handle null arguments gracefully");
            }
        }

        private async Task TestEmptyPath(WriteFileTool tool)
        {
            var args = new WriteFileArguments
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

        private async Task TestInvalidPath(WriteFileTool tool)
        {
            var args = new WriteFileArguments
            {
                Path = "invalid|path<>?.txt",
                Content = "test content"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error writing file"))
            {
                throw new Exception("Should handle invalid path characters");
            }
        }
    }
} 