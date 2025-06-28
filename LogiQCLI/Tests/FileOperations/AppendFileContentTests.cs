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
    public class AppendFileContentTests : TestBase
    {
        public override string TestName => "AppendFileTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new AppendFileTool();
            var testFileSystem = new TestFileSystem();

            try
            {
                await TestAppendToNewFile(tool, testFileSystem);
                await TestAppendToExistingFileWithNewline(tool, testFileSystem);
                await TestAppendToExistingFileWithoutNewline(tool, testFileSystem);
                await TestAppendToEmptyFile(tool, testFileSystem);
                await TestLargeContentAppend(tool, testFileSystem);
                await TestUnicodeAppend(tool, testFileSystem);
                await TestPathSeparators(tool, testFileSystem);
                await TestMultipleAppends(tool, testFileSystem);

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

        private async Task TestAppendToNewFile(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var content = "Initial content in new file.";
            var path = Path.Combine(testFileSystem.TempDirectory, "append_new_test.txt");

            var args = new AppendFileArguments
            {
                Path = path.Replace(Path.DirectorySeparatorChar, '/'),
                Content = content
            };

            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);

            if (!result.Contains("Successfully appended"))
            {
                throw new Exception($"Expected success message. Got: {result}");
            }

            if (!File.Exists(path))
            {
                throw new Exception("File should be created when it does not exist");
            }

            var actual = await File.ReadAllTextAsync(path);
            if (actual != content)
            {
                throw new Exception("Content should match exactly for first append");
            }
        }

        private async Task TestAppendToExistingFileWithNewline(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var initial = "Line one";
            var extra = "Line two";
            var path = testFileSystem.CreateTempFile(initial, "append_existing_nl.txt");

            var args = new AppendFileArguments
            {
                Path = path,
                Content = extra,
                Newline = true
            };

            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);

            var expected = initial + Environment.NewLine + extra;
            var actual = await File.ReadAllTextAsync(path);
            if (actual != expected)
            {
                throw new Exception("Append with newline should insert newline before content");
            }
        }

        private async Task TestAppendToExistingFileWithoutNewline(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var initial = "ABC";
            var extra = "DEF";
            var path = testFileSystem.CreateTempFile(initial, "append_existing_nonl.txt");

            var args = new AppendFileArguments
            {
                Path = path,
                Content = extra,
                Newline = false
            };

            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);

            var expected = initial + extra;
            var actual = await File.ReadAllTextAsync(path);
            if (actual != expected)
            {
                throw new Exception("Append without newline should concatenate directly");
            }
        }

        private async Task TestAppendToEmptyFile(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var path = testFileSystem.CreateTempFile(string.Empty, "append_empty.txt");
            var extra = "Content after empty";

            var args = new AppendFileArguments
            {
                Path = path,
                Content = extra,
                Newline = true
            };

            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);

            var actual = await File.ReadAllTextAsync(path);
            if (actual != extra)
            {
                throw new Exception("Newline should not be added to empty file");
            }
        }

        private async Task TestLargeContentAppend(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var initial = new string('X', 1024);
            var largeExtra = new string('Y', 2048);
            var path = testFileSystem.CreateTempFile(initial, "append_large.txt");

            var args = new AppendFileArguments
            {
                Path = path,
                Content = largeExtra,
                Newline = false
            };

            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);

            var expected = initial + largeExtra;
            var actual = await File.ReadAllTextAsync(path);
            if (actual.Length != expected.Length)
            {
                throw new Exception("File length should match after large append");
            }
            if (actual != expected)
            {
                throw new Exception("Content should match after large append");
            }
        }

        private async Task TestUnicodeAppend(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var initial = "Hello";
            var unicodeExtra = " 世界 🚀";
            var path = testFileSystem.CreateTempFile(initial, "append_unicode.txt");

            var args = new AppendFileArguments
            {
                Path = path,
                Content = unicodeExtra,
                Newline = false
            };

            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);

            var expected = initial + unicodeExtra;
            var actual = await File.ReadAllTextAsync(path);
            if (actual != expected)
            {
                throw new Exception("Unicode characters should append correctly");
            }
        }

        private async Task TestPathSeparators(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var initial = "Separator test";
            var extra = " - extra";
            var subdir = Path.Combine(testFileSystem.TempDirectory, "sep");
            Directory.CreateDirectory(subdir);
            var filePath = Path.Combine(subdir, "append_sep.txt");
            File.WriteAllText(filePath, initial);

            var relativePath = filePath.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "").Replace(Path.DirectorySeparatorChar, '/');

            var args = new AppendFileArguments
            {
                Path = relativePath,
                Content = extra,
                Newline = false
            };

            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);

            var expected = initial + extra;
            var actual = await File.ReadAllTextAsync(filePath);
            if (actual != expected)
            {
                throw new Exception("Tool should handle forward slash path separators");
            }
        }

        private async Task TestMultipleAppends(AppendFileTool tool, TestFileSystem testFileSystem)
        {
            var path = testFileSystem.CreateTempFile("Start", "append_multi.txt");

            for (int i = 0; i < 5; i++)
            {
                var args = new AppendFileArguments
                {
                    Path = path,
                    Content = $"_{i}",
                    Newline = false
                };
                var json = JsonSerializer.Serialize(args);
                await tool.Execute(json);
            }

            var expected = "Start_0_1_2_3_4";
            var actual = await File.ReadAllTextAsync(path);
            if (actual != expected)
            {
                throw new Exception("Consecutive appends should produce cumulative content");
            }
        }
    }
} 