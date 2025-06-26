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
    public class WriteFileContentTests : TestBase
    {
        public override string TestName => "WriteFileTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new WriteFileTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestBasicFileWrite(tool, testFileSystem);
                await TestEmptyContentWrite(tool, testFileSystem);
                await TestNullContentWrite(tool, testFileSystem);
                await TestOverwriteExistingFile(tool, testFileSystem);
                await TestUnicodeContent(tool, testFileSystem);
                await TestLargeContent(tool, testFileSystem);
                await TestDirectoryCreation(tool, testFileSystem);
                await TestMultilineContent(tool, testFileSystem);
                
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

        private async Task TestBasicFileWrite(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello, World! This is a test file.";
            var testPath = Path.Combine(testFileSystem.TempDirectory, "basic_write_test.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote") || !result.Contains("34 characters"))
            {
                throw new Exception($"Should report successful write. Got: {result}");
            }
            
            if (!File.Exists(testPath))
            {
                throw new Exception("File should be created");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != testContent)
            {
                throw new Exception($"File content mismatch. Expected: '{testContent}', Got: '{actualContent}'");
            }
        }

        private async Task TestEmptyContentWrite(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "empty_write_test.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = ""
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote") || !result.Contains("0 characters"))
            {
                throw new Exception("Should handle empty content correctly");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != "")
            {
                throw new Exception("Empty file should be empty");
            }
        }

        private async Task TestNullContentWrite(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "null_content_test.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = null
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote") || !result.Contains("0 characters"))
            {
                throw new Exception("Should handle null content as empty string");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != "")
            {
                throw new Exception("Null content should result in empty file");
            }
        }

        private async Task TestOverwriteExistingFile(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "overwrite_test.txt");
            var originalContent = "Original content";
            var newContent = "New content that replaces original";
            

            await File.WriteAllTextAsync(testPath, originalContent);
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = newContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote"))
            {
                throw new Exception("Should overwrite existing file successfully");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != newContent)
            {
                throw new Exception($"Content should be replaced. Expected: '{newContent}', Got: '{actualContent}'");
            }
        }

        private async Task TestUnicodeContent(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello 世界! Café naïve résumé 🚀💻";
            var testPath = Path.Combine(testFileSystem.TempDirectory, "unicode_write_test.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote"))
            {
                throw new Exception("Should handle unicode content");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != testContent)
            {
                throw new Exception("Unicode content should be preserved");
            }
        }

        private async Task TestLargeContent(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = new string('A', 2048);
            var testPath = Path.Combine(testFileSystem.TempDirectory, "large_write_test.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote") || !result.Contains("2048 characters"))
            {
                throw new Exception("Should handle large content correctly");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent.Length != 2048)
            {
                throw new Exception($"Content length mismatch. Expected: 2048, Got: {actualContent.Length}");
            }
        }

        private async Task TestDirectoryCreation(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Directory creation test";
            var subdirPath = Path.Combine(testFileSystem.TempDirectory, "newdir", "nested", "deep");
            var testPath = Path.Combine(subdirPath, "nested_file.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote"))
            {
                throw new Exception("Should create directories and file");
            }
            
            if (!Directory.Exists(subdirPath))
            {
                throw new Exception("Should create nested directories");
            }
            
            if (!File.Exists(testPath))
            {
                throw new Exception("Should create file in nested directory");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != testContent)
            {
                throw new Exception("Content should be correct in nested file");
            }
        }

        private async Task TestMultilineContent(WriteFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Line 1\nLine 2\r\nLine 3\nLast line";
            var testPath = Path.Combine(testFileSystem.TempDirectory, "multiline_write_test.txt");
            
            var args = new WriteFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully wrote"))
            {
                throw new Exception("Should handle multiline content");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != testContent)
            {
                throw new Exception("Should preserve line endings in multiline content");
            }
        }
    }
} 
