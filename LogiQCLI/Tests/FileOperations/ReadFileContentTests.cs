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
    public class ReadFileContentTests : TestBase
    {
        public override string TestName => "ReadFileTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new ReadFileTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestBasicFileRead(tool, testFileSystem);
                await TestEmptyFileRead(tool, testFileSystem);
                await TestLargeFileRead(tool, testFileSystem);
                await TestUnicodeContent(tool, testFileSystem);
                await TestMultilineContent(tool, testFileSystem);
                await TestPathSeparators(tool, testFileSystem);
                
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

        private async Task TestBasicFileRead(ReadFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello, World! This is a test file.";
            var testFile = testFileSystem.CreateTempFile(testContent, "basic_test.txt");
            
            var args = new ReadFileArguments
            {
                Path = testFile
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result != testContent)
            {
                throw new Exception($"Should return exact file content. Expected: '{testContent}', Got: '{result}'");
            }
        }

        private async Task TestEmptyFileRead(ReadFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "";
            var testFile = testFileSystem.CreateTempFile(testContent, "empty_test.txt");
            
            var args = new ReadFileArguments
            {
                Path = testFile
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result != "")
            {
                throw new Exception("Should return empty string for empty file");
            }
        }

        private async Task TestLargeFileRead(ReadFileTool tool, TestFileSystem testFileSystem)
        {
            // Create a file with ~1KB of content
            var testContent = new string('A', 1024);
            var testFile = testFileSystem.CreateTempFile(testContent, "large_test.txt");
            
            var args = new ReadFileArguments
            {
                Path = testFile
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result != testContent)
            {
                throw new Exception("Should handle large files correctly");
            }
            
            if (result.Length != 1024)
            {
                throw new Exception($"Content length mismatch. Expected: 1024, Got: {result.Length}");
            }
        }

        private async Task TestUnicodeContent(ReadFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello 世界! Café naïve résumé 🚀💻";
            var testFile = testFileSystem.CreateTempFile(testContent, "unicode_test.txt");
            
            var args = new ReadFileArguments
            {
                Path = testFile
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result != testContent)
            {
                throw new Exception("Should handle unicode content correctly");
            }
        }

        private async Task TestMultilineContent(ReadFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Line 1\nLine 2\r\nLine 3\nLast line";
            var testFile = testFileSystem.CreateTempFile(testContent, "multiline_test.txt");
            
            var args = new ReadFileArguments
            {
                Path = testFile
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result != testContent)
            {
                throw new Exception("Should preserve line endings and multiline content");
            }
        }

        private async Task TestPathSeparators(ReadFileTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Path separator test";
            var subdirPath = Path.Combine(testFileSystem.TempDirectory, "subdir");
            Directory.CreateDirectory(subdirPath);
            var testFile = Path.Combine(subdirPath, "path_test.txt");
            File.WriteAllText(testFile, testContent);
            
            // Test with forward slashes (should work on all platforms)
            var relativePath = testFile.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "")
                                       .Replace(Path.DirectorySeparatorChar, '/');
            
            var args = new ReadFileArguments
            {
                Path = relativePath
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (result != testContent)
            {
                throw new Exception("Should handle forward slash path separators correctly");
            }
        }
    }
} 