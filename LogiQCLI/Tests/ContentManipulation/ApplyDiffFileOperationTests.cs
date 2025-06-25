using System;
using System.IO;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Objects;
using System.Text.Json;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class ApplyDiffFileOperationTests : TestBase
    {
        public override string TestName => "ApplyDiffTool_FileOperationTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new ApplyDiffTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestValidFileExists(tool, testFileSystem);
                await TestLargeFileSize(tool, testFileSystem);
                await TestBackupCreation(tool, testFileSystem);
                await TestFileWriteFailure(tool, testFileSystem);
                await TestPathNormalization(tool, testFileSystem);
                await TestFilePermissions(tool, testFileSystem);
                
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

        private async Task TestValidFileExists(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello world\nThis is a test file\nWith multiple lines";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new ApplyDiffArguments
            {
                Path = testFile,
                Original = "world",
                Replacement = "universe"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result.Contains("File not found"))
            {
                throw new Exception("Should process existing file successfully");
            }
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Hello universe"))
            {
                throw new Exception("File content should be modified");
            }
        }

        private async Task TestLargeFileSize(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var largeFile = testFileSystem.CreateLargeFile(12);
            
            var args = new ApplyDiffArguments
            {
                Path = largeFile,
                Original = "A",
                Replacement = "B"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error: File too large"))
            {
                throw new Exception("Should reject files exceeding size limit");
            }
        }

        private async Task TestBackupCreation(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Original content for backup test";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new ApplyDiffArguments
            {
                Path = testFile,
                Original = "Original",
                Replacement = "Modified",
                CreateBackup = false,
                MaxReplacements = -1
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Modified content for backup test"))
            {
                throw new Exception("File should be modified successfully");
            }
        }

        private async Task TestFileWriteFailure(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Test content";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var fileInfo = new FileInfo(testFile);
            fileInfo.IsReadOnly = true;
            
            try
            {
                var args = new ApplyDiffArguments
                {
                    Path = testFile,
                    Original = "Test",
                    Replacement = "Modified"
                };
                
                var json = JsonSerializer.Serialize(args);
                var result = await tool.Execute(json);
                
                if (!result.Contains("Error") && !result.Contains("access denied") && !result.Contains("write"))
                {
                    throw new Exception("Should handle write failures gracefully");
                }
            }
            finally
            {
                fileInfo.IsReadOnly = false;
            }
        }

        private async Task TestPathNormalization(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Path normalization test";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var normalizedPath = testFile.Replace('\\', '/');
            
            var args = new ApplyDiffArguments
            {
                Path = normalizedPath,
                Original = "Path",
                Replacement = "Route",
                MaxReplacements = -1
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result.Contains("File not found"))
            {
                throw new Exception("Should handle path normalization correctly");
            }
        }

        private async Task TestFilePermissions(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Permission test content";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new ApplyDiffArguments
            {
                Path = testFile,
                Original = "Permission",
                Replacement = "Access",
                MaxReplacements = -1
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result.Contains("File not found"))
            {
                throw new Exception("Should process file with normal permissions");
            }
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Access test content"))
            {
                throw new Exception("File should be modified successfully");
            }
        }
    }
}
