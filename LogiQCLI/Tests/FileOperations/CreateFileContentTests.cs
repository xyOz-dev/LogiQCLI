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
    public class CreateFileContentTests : TestBase
    {
        public override string TestName => "CreateFileTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new CreateFileTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestBasicFileCreation(tool, testFileSystem);
                await TestEmptyFileCreation(tool, testFileSystem);
                await TestNullContentCreation(tool, testFileSystem);
                await TestExistingFileWithoutOverwrite(tool, testFileSystem);
                await TestExistingFileWithOverwrite(tool, testFileSystem);
                await TestDirectoryCreation(tool, testFileSystem);
                await TestUnicodeContent(tool, testFileSystem);
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

        private async Task TestBasicFileCreation(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello, World! This is a newly created file.";
            var testPath = Path.Combine(testFileSystem.TempDirectory, "basic_create_test.txt");
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Created file") || !result.Contains("43 characters"))
            {
                throw new Exception($"Should report successful creation. Got: {result}");
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

        private async Task TestEmptyFileCreation(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "empty_create_test.txt");
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = ""
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Created file") || !result.Contains("0 characters"))
            {
                throw new Exception("Should create empty file correctly");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != "")
            {
                throw new Exception("Empty file should be empty");
            }
        }

        private async Task TestNullContentCreation(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "null_content_create_test.txt");
            
            var args = new CreateFileArguments
            {
                Path = testPath
                // Content is null by default
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Created file") || !result.Contains("0 characters"))
            {
                throw new Exception("Should handle null content as empty string");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != "")
            {
                throw new Exception("Null content should result in empty file");
            }
        }

        private async Task TestExistingFileWithoutOverwrite(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "existing_test.txt");
            var originalContent = "Original content";
            
            // Create initial file
            await File.WriteAllTextAsync(testPath, originalContent);
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = "New content",
                Overwrite = false
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Error") || !result.Contains("File already exists"))
            {
                throw new Exception("Should reject creating over existing file without overwrite flag");
            }
            
            // Original content should be preserved
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != originalContent)
            {
                throw new Exception("Original content should be preserved when creation fails");
            }
        }

        private async Task TestExistingFileWithOverwrite(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testPath = Path.Combine(testFileSystem.TempDirectory, "overwrite_create_test.txt");
            var originalContent = "Original content";
            var newContent = "New content that replaces original";
            
            // Create initial file
            await File.WriteAllTextAsync(testPath, originalContent);
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = newContent,
                Overwrite = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Overwrote file"))
            {
                throw new Exception("Should report overwriting existing file");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != newContent)
            {
                throw new Exception($"Content should be replaced. Expected: '{newContent}', Got: '{actualContent}'");
            }
        }

        private async Task TestDirectoryCreation(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Directory creation test";
            var subdirPath = Path.Combine(testFileSystem.TempDirectory, "newdir", "nested", "deep");
            var testPath = Path.Combine(subdirPath, "nested_file.txt");
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Created file"))
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

        private async Task TestUnicodeContent(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello 世界! Café naïve résumé 🚀💻";
            var testPath = Path.Combine(testFileSystem.TempDirectory, "unicode_create_test.txt");
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Created file"))
            {
                throw new Exception("Should handle unicode content");
            }
            
            var actualContent = await File.ReadAllTextAsync(testPath);
            if (actualContent != testContent)
            {
                throw new Exception("Unicode content should be preserved");
            }
        }

        private async Task TestMultilineContent(CreateFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Line 1\nLine 2\r\nLine 3\nLast line";
            var testPath = Path.Combine(testFileSystem.TempDirectory, "multiline_create_test.txt");
            
            var args = new CreateFileArguments
            {
                Path = testPath,
                Content = testContent
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Created file"))
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