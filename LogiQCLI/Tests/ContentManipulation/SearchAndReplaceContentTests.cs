using System;
using System.IO;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Objects;
using System.Text.Json;
using LogiQCLI.Tools.Core;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class SearchAndReplaceContentTests : TestBase
    {
        public override string TestName => "SearchAndReplaceTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new SearchAndReplaceTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestBasicReplacement(tool, testFileSystem);
                await TestMultipleOccurrences(tool, testFileSystem);
                await TestCaseSensitiveReplacement(tool, testFileSystem);
                await TestCaseInsensitiveReplacement(tool, testFileSystem);
                await TestUnicodeReplacement(tool, testFileSystem);
                await TestEmptyReplacement(tool, testFileSystem);
                await TestNoMatches(tool, testFileSystem);
                await TestRegexBasic(tool, testFileSystem);
                await TestRegexCaptureGroups(tool, testFileSystem);
                await TestRegexMultiline(tool, testFileSystem);
                await TestBackupCreation(tool, testFileSystem);
                await TestNoBackup(tool, testFileSystem);
                
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

        private async Task TestBasicReplacement(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello world! This is a test world.";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "world",
                Replace = "universe"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully replaced"))
            {
                throw new Exception("Should indicate successful replacement");
            }
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Hello universe!") || !modifiedContent.Contains("test universe."))
            {
                throw new Exception("Basic string replacement failed");
            }
        }

        private async Task TestMultipleOccurrences(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "test test test test test";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "test",
                Replace = "exam"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Successfully replaced 5 occurrence(s)"))
            {
                throw new Exception("Should replace all occurrences and report count");
            }
            
            var modifiedContent = File.ReadAllText(testFile);
            if (modifiedContent != "exam exam exam exam exam")
            {
                throw new Exception("Multiple replacement failed");
            }
        }

        private async Task TestCaseSensitiveReplacement(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Test test TEST tEsT";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "test",
                Replace = "exam",
                CaseSensitive = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Test exam TEST tEsT"))
            {
                throw new Exception("Case sensitive replacement should only replace exact matches");
            }
        }

        private async Task TestCaseInsensitiveReplacement(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Test test TEST tEsT";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "test",
                Replace = "exam",
                CaseSensitive = false
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (modifiedContent != "exam exam exam exam")
            {
                throw new Exception("Case insensitive replacement should replace all case variants");
            }
        }

        private async Task TestUnicodeReplacement(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello 世界! Testing unicode: café, naïve, résumé";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "世界",
                Replace = "universe"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Hello universe!"))
            {
                throw new Exception("Unicode replacement failed");
            }
        }

        private async Task TestEmptyReplacement(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Remove this word from the text";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "word ",
                Replace = ""
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (modifiedContent != "Remove this from the text")
            {
                throw new Exception("Empty replacement (deletion) failed");
            }
        }

        private async Task TestNoMatches(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "This content has no matches";
            var originalContent = testContent;
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "nonexistent",
                Replace = "replacement"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("No matches found"))
            {
                throw new Exception("Should report when no matches are found");
            }
            
            var modifiedContent = File.ReadAllText(testFile);
            if (modifiedContent != originalContent)
            {
                throw new Exception("Content should remain unchanged when no matches found");
            }
        }

        private async Task TestRegexBasic(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Replace all numbers: 123, 456, 789";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = @"\d+",
                Replace = "XXX",
                UseRegex = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (modifiedContent != "Replace all numbers: XXX, XXX, XXX")
            {
                throw new Exception("Basic regex replacement failed");
            }
        }

        private async Task TestRegexCaptureGroups(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Change order: firstName lastName";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = @"(\w+)\s+(\w+)",
                Replace = "$2, $1",
                UseRegex = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("lastName, firstName"))
            {
                throw new Exception("Regex capture group replacement failed");
            }
        }

        private async Task TestRegexMultiline(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Line 1\nStart: some content\nLine 3";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = @"^Start:",
                Replace = "Begin:",
                UseRegex = true,
                Multiline = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Begin: some content"))
            {
                throw new Exception("Regex multiline replacement failed");
            }
        }

        private async Task TestBackupCreation(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Original content for backup test";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "Original",
                Replace = "Modified",
                Backup = true
            };
            
            var json = JsonSerializer.Serialize(args);

            var backupManager = new LogiqBackupManager();
            var beforeCount = backupManager.ListBackups(testFile).Count;

            var result = await tool.Execute(json);

            var afterCount = new LogiqBackupManager().ListBackups(testFile).Count;
            if (afterCount != beforeCount + 1)
            {
                throw new Exception("A new backup entry should be created when backup=true");
            }

            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Modified content for backup test"))
            {
                throw new Exception("File should be modified successfully");
            }
        }

        private async Task TestNoBackup(SearchAndReplaceTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Original content for no backup test";
            var testFile = testFileSystem.CreateTempFile(testContent);
            
            var args = new SearchAndReplaceArguments
            {
                Path = testFile,
                Search = "Original",
                Replace = "Modified",
                Backup = false
            };
            
            var json = JsonSerializer.Serialize(args);

            var backupManager = new LogiqBackupManager();
            var beforeCount = backupManager.ListBackups(testFile).Count;

            var result = await tool.Execute(json);

            var afterCount = new LogiqBackupManager().ListBackups(testFile).Count;
            if (afterCount != beforeCount)
            {
                throw new Exception("No backup entry should be created when backup=false");
            }

            var modifiedContent = File.ReadAllText(testFile);
            if (!modifiedContent.Contains("Modified content for no backup test"))
            {
                throw new Exception("File should be modified successfully");
            }
        }
    }
}
