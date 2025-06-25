using System;
using System.IO;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Arguments;
using System.Text.Json;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class SearchFilesContentTests : TestBase
    {
        public override string TestName => "SearchFilesTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new SearchFilesTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestBasicTextSearch(tool, testFileSystem);
                await TestCaseSensitiveSearch(tool, testFileSystem);
                await TestCaseInsensitiveSearch(tool, testFileSystem);
                await TestRegexSearch(tool, testFileSystem);
                await TestFilePatternFilter(tool, testFileSystem);
                await TestMaxResultsLimit(tool, testFileSystem);
                await TestNoMatches(tool, testFileSystem);
                await TestMultipleFiles(tool, testFileSystem);
                await TestUnicodeSearch(tool, testFileSystem);
                await TestPathFilter(tool, testFileSystem);
                
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

        private async Task TestBasicTextSearch(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "This is a test file\nwith some test content\nfor testing purposes";
            var testFile = testFileSystem.CreateTempFile(testContent, "test1.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "test",
                Path = testFileSystem.TempDirectory
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found"))
            {
                throw new Exception("Should find matches and report count");
            }
            
            if (!result.Contains("test1.txt"))
            {
                throw new Exception("Should include filename in results");
            }
            
            if (!result.Contains("This is a test file"))
            {
                throw new Exception("Should include matching line content");
            }
        }

        private async Task TestCaseSensitiveSearch(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Test case_sensitive_test TEST tEsT";
            var testFile = testFileSystem.CreateTempFile(testContent, "case_sensitive_only.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "case_sensitive_test",
                Path = testFileSystem.TempDirectory,
                CaseSensitive = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 1 matches"))
            {
                throw new Exception($"Case sensitive search should only find exact matches. Got result: {result}");
            }
            
            if (!result.Contains("case_sensitive_only.txt"))
            {
                throw new Exception("Should find match in the correct file");
            }
        }

        private async Task TestCaseInsensitiveSearch(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "CASE_INSENSITIVE\ncase_insensitive\nCase_Insensitive\ncAsE_iNsEnSiTiVe";
            var testFile = testFileSystem.CreateTempFile(testContent, "case_insensitive_only.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "case_insensitive",
                Path = testFileSystem.TempDirectory,
                CaseSensitive = false
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 4 matches"))
            {
                throw new Exception($"Case insensitive search should find all case variants. Got result: {result}");
            }
        }

        private async Task TestRegexSearch(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Email: user@example.com\nAnother email: admin@test.org\nNot an email: invalid";
            var testFile = testFileSystem.CreateTempFile(testContent, "emails.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                Path = testFileSystem.TempDirectory,
                UseRegex = true
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 2 matches"))
            {
                throw new Exception("Regex search should find email patterns");
            }
            
            if (!result.Contains("user@example.com") || !result.Contains("admin@test.org"))
            {
                throw new Exception("Should find both email addresses");
            }
        }

        private async Task TestFilePatternFilter(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Some filter_test_content to search";
            testFileSystem.CreateTempFile(testContent, "filter1.txt");
            testFileSystem.CreateTempFile(testContent, "filter2.cs");
            testFileSystem.CreateTempFile(testContent, "filter3.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "filter_test_content",
                Path = testFileSystem.TempDirectory,
                FilePattern = "*.txt"
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 2 matches"))
            {
                throw new Exception($"File pattern filter should only search matching files. Got result: {result}");
            }
            
            if (result.Contains("filter2.cs"))
            {
                throw new Exception("Should not search files that don't match file pattern");
            }
        }

        private async Task TestMaxResultsLimit(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "line 1\nline 2\nline 3\nline 4\nline 5";
            var testFile = testFileSystem.CreateTempFile(testContent, "lines.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "line",
                Path = testFileSystem.TempDirectory,
                MaxResults = 3
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 3 matches"))
            {
                throw new Exception("Should limit results to max_results parameter");
            }
            
            if (!result.Contains("(Limited to first 3 results)"))
            {
                throw new Exception("Should indicate when results are limited");
            }
        }

        private async Task TestNoMatches(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "This file has no special patterns";
            var testFile = testFileSystem.CreateTempFile(testContent, "no_match.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "nonexistent_pattern",
                Path = testFileSystem.TempDirectory
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("No matches found"))
            {
                throw new Exception("Should report when no matches are found");
            }
        }

        private async Task TestMultipleFiles(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            testFileSystem.CreateTempFile("TODO: Fix this bug", "file1.txt");
            testFileSystem.CreateTempFile("Regular content here", "file2.txt");
            testFileSystem.CreateTempFile("TODO: Add feature", "file3.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "TODO:",
                Path = testFileSystem.TempDirectory
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 2 matches"))
            {
                throw new Exception("Should find matches across multiple files");
            }
            
            if (!result.Contains("file1.txt") || !result.Contains("file3.txt"))
            {
                throw new Exception("Should include both files with matches");
            }
            
            if (result.Contains("file2.txt"))
            {
                throw new Exception("Should not include files without matches");
            }
        }

        private async Task TestUnicodeSearch(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var testContent = "Hello 世界! This contains unicode: café, naïve, résumé";
            var testFile = testFileSystem.CreateTempFile(testContent, "unicode.txt");
            
            var args = new SearchFilesArguments
            {
                Pattern = "世界",
                Path = testFileSystem.TempDirectory
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 1 matches"))
            {
                throw new Exception("Unicode search should work correctly");
            }
            
            if (!result.Contains("世界"))
            {
                throw new Exception("Should display unicode content correctly");
            }
        }

        private async Task TestPathFilter(SearchFilesTool tool, TestFileSystem testFileSystem)
        {
            var subDir = Path.Combine(testFileSystem.TempDirectory, "subdir");
            Directory.CreateDirectory(subDir);
            
            testFileSystem.CreateTempFile("root content", "root.txt");
            File.WriteAllText(Path.Combine(subDir, "sub.txt"), "sub content");
            
            var args = new SearchFilesArguments
            {
                Pattern = "content",
                Path = subDir
            };
            
            var json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            
            if (!result.Contains("Found 1 matches"))
            {
                throw new Exception("Path filter should limit search to specified directory");
            }
            
            if (!result.Contains("sub.txt"))
            {
                throw new Exception("Should find file in specified directory");
            }
            
            if (result.Contains("root.txt"))
            {
                throw new Exception("Should not find files outside specified directory");
            }
        }
    }
} 
