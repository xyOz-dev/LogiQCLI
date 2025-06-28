using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.SystemOperations;
using LogiQCLI.Tools.SystemOperations.Objects;

namespace LogiQCLI.Tests.SystemOperations
{
    public class ListFilesToolTests : TestBase
    {
        public override string TestName => "ListFilesTool_Tests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new ListFilesTool();
            var testFileSystem = new TestFileSystem();
            var originalDirectory = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(testFileSystem.TempDirectory);

                await TestNonRecursiveListing(tool, testFileSystem);
                await TestRecursiveListing(tool, testFileSystem);
                await TestDirectoryNotExists(tool, testFileSystem);
                await TestInvalidArguments(tool);

                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
                testFileSystem.Dispose();
            }
        }

        private async Task TestNonRecursiveListing(ListFilesTool tool, TestFileSystem fs)
        {
            fs.CreateTempFile("content", "file1.txt");
            fs.CreateTempFile("content", "file2.txt");

            var subDir = Path.Combine(fs.TempDirectory, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

            var args = new ListFilesArguments
            {
                Path = ".",
                Recursive = false
            };

            var result = await tool.Execute(JsonSerializer.Serialize(args));

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var expected = new[] { "file1.txt", "file2.txt", "sub" };

            foreach (var path in expected)
            {
                if (!lines.Contains(path))
                {
                    throw new Exception($"Non-recursive listing missing entry {path}");
                }
            }

            if (lines.Any(l => l.StartsWith($"sub{Path.DirectorySeparatorChar}")))
            {
                throw new Exception("Non-recursive listing should not include nested files");
            }
        }

        private async Task TestRecursiveListing(ListFilesTool tool, TestFileSystem fs)
        {
            var args = new ListFilesArguments
            {
                Path = ".",
                Recursive = true
            };

            var result = await tool.Execute(JsonSerializer.Serialize(args));

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var expectedNested = Path.Combine("sub", "nested.txt");

            if (!lines.Contains(expectedNested))
            {
                throw new Exception("Recursive listing should include nested files");
            }

            var expectedTopLevel = new[] { "file1.txt", "file2.txt", "sub" };
            foreach (var path in expectedTopLevel)
            {
                if (!lines.Contains(path))
                {
                    throw new Exception($"Recursive listing missing top-level entry {path}");
                }
            }
        }

        private async Task TestDirectoryNotExists(ListFilesTool tool, TestFileSystem fs)
        {
            var nonExisting = Path.Combine(fs.TempDirectory, "does_not_exist");
            var args = new ListFilesArguments
            {
                Path = nonExisting
            };

            var result = await tool.Execute(JsonSerializer.Serialize(args));

            if (!result.StartsWith("Error: Directory does not exist"))
            {
                throw new Exception("Should return error when directory does not exist");
            }
        }

        private async Task TestInvalidArguments(ListFilesTool tool)
        {
            var result = await tool.Execute("{}");

            if (!result.StartsWith("Error: Invalid arguments"))
            {
                throw new Exception("Should return error for missing path argument");
            }
        }
    }
} 