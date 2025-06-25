using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.FileOperations;
using LogiQCLI.Tools.FileOperations.Arguments;

namespace LogiQCLI.Tests.FileOperations
{
    public class ReadFileValidationTests : TestBase
    {
        public override string TestName => "ReadFileTool_ValidationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new ReadFileTool();
            
            try
            {
                await TestNullArguments(tool);
                await TestEmptyPath(tool);
                await TestNonExistentFile(tool);
                await TestInvalidPath(tool);
                
                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
        }

        private async Task TestNullArguments(ReadFileTool tool)
        {
            var result = await tool.Execute(null);
            
            if (!result.Contains("Error"))
            {
                throw new Exception("Should handle null arguments gracefully");
            }
        }

        private async Task TestEmptyPath(ReadFileTool tool)
        {
            var args = new ReadFileArguments
            {
                Path = ""
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error") || !result.Contains("Path is required"))
            {
                throw new Exception("Should reject empty path");
            }
        }

        private async Task TestNonExistentFile(ReadFileTool tool)
        {
            var args = new ReadFileArguments
            {
                Path = "nonexistent_file_12345.txt"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error reading file"))
            {
                throw new Exception("Should handle non-existent file gracefully");
            }
        }

        private async Task TestInvalidPath(ReadFileTool tool)
        {
            var args = new ReadFileArguments
            {
                Path = "invalid|path<>?.txt"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error reading file"))
            {
                throw new Exception("Should handle invalid path characters");
            }
        }
    }
} 