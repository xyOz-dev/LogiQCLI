using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.ContentManipulation.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core;

namespace LogiQCLI.Tools.ContentManipulation
{
    [ToolMetadata("ContentManipulation", Tags = new[] { "write" })]
    public class ApplyDiffTool : ITool
    {
        private const int MaxFileSizeBytes = 10 * 1024 * 1024;
        
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "apply_diff",
                Description = "Apply direct content replacements. Use for edits when you know the exact content to change.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace. File must exist and be under 10MB."
                        },
                        original = new
                        {
                            type = "string",
                            description = "Exact content to find and replace. Must match precisely including whitespace unless useRegex=true."
                        },
                        replacement = new
                        {
                            type = "string",
                            description = "New content to replace original with. Empty string deletes the content."
                        },
                        maxReplacements = new
                        {
                            type = "number",
                            description = "Max replacements to perform. Default: 1 (first match only). Use -1 for all occurrences."
                        },
                        useRegex = new
                        {
                            type = "boolean",
                            description = "Treat 'original' as regex pattern. Default: false. Enables multiline mode."
                        },
                        preview = new
                        {
                            type = "boolean",
                            description = "Show changes without applying. Default: false."
                        }
                    },
                    Required = new[] { "path", "original", "replacement" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ApplyDiffArguments>(args);
                if (arguments == null)
                {
                    return "Error: Invalid arguments provided.";
                }
                
                var validationError = ValidateArguments(arguments);
                if (validationError != null)
                {
                    return validationError;
                }

                var fullPath = GetValidFilePath(arguments.Path);
                if (fullPath == null)
                {
                    return $"Error: File not found: {arguments.Path}. Current directory: {Directory.GetCurrentDirectory()}";
                }

                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    return $"Error: File too large ({fileInfo.Length:N0} bytes). Maximum supported size is {MaxFileSizeBytes:N0} bytes.";
                }

                string originalContent;
                using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(fileStream))
                {
                    originalContent = await reader.ReadToEndAsync();
                }

                var lineEndingStyle = DetectLineEndingStyle(originalContent);
                var normalizedContent = NormalizeLineEndings(originalContent);
                var normalizedOriginal = NormalizeLineEndings(arguments.Original ?? string.Empty);
                var normalizedReplacement = NormalizeLineEndings(arguments.Replacement ?? string.Empty);

                var result = arguments.UseRegex
                    ? ApplyRegexReplacement(normalizedContent, normalizedOriginal, normalizedReplacement, arguments.MaxReplacements)
                    : ApplyStringReplacement(normalizedContent, normalizedOriginal, normalizedReplacement, arguments.MaxReplacements);

                if (!result.Success)
                {
                    return $"Error: {result.ErrorMessage}\nFile preview (first 200 chars):\n{originalContent.Substring(0, Math.Min(200, originalContent.Length))}...";
                }

                if (arguments.Preview)
                {
                    return $"Preview mode - would make {result.ReplacementCount} replacement(s):\n{GeneratePreview(originalContent, result.ModifiedContent, result.MatchPositions)}";
                }

                var finalContent = RestoreLineEndings(result.ModifiedContent, lineEndingStyle);

                string? backupPath = null;
                if (arguments.CreateBackup)
                {
                    backupPath = CreateBackup(fullPath, originalContent);
                }

                try
                {
                    using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fileStream))
                    {
                        await writer.WriteAsync(finalContent);
                    }

                    var successMessage = $"Successfully applied diff to {fullPath}. {result.ReplacementCount} replacement(s) made.";
                    if (backupPath != null)
                    {
                        successMessage += $" Backup created: {backupPath}";
                    }
                    return successMessage;
                }
                catch (Exception writeEx)
                {
                    if (backupPath != null)
                    {
                        try
                        {
                            File.Copy(backupPath, fullPath, true);
                            return $"Error writing file: {writeEx.Message}. File restored from backup.";
                        }
                        catch
                        {
                            return $"Error writing file: {writeEx.Message}. WARNING: Could not restore from backup at {backupPath}";
                        }
                    }
                    return $"Error writing file: {writeEx.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Error applying diff: {ex.Message}";
            }
        }

        private string? ValidateArguments(ApplyDiffArguments arguments)
        {
            if (arguments == null)
                return "Error: Invalid arguments provided.";
                
            if (string.IsNullOrEmpty(arguments.Path))
                return "Error: Path is required.";
                
            if (arguments.Original == null)
                return "Error: Original content cannot be null.";
                
            if (arguments.Replacement == null)
                return "Error: Replacement content cannot be null.";
                
            if (string.IsNullOrEmpty(arguments.Original))
                return "Error: Original content cannot be empty (use empty string for replacement if deletion is intended).";

            if (arguments.MaxReplacements == 0)
                return "Error: MaxReplacements cannot be 0. Use -1 for all replacements or positive number for specific count.";

            if (arguments.UseRegex)
            {
                try
                {
                    new Regex(arguments.Original);
                }
                catch (ArgumentException ex)
                {
                    return $"Error: Invalid regex pattern in original content: {ex.Message}";
                }
            }

            return null;
        }

        private string? GetValidFilePath(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return null;

            var fullPath = Path.GetFullPath(inputPath.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar));
            
            return File.Exists(fullPath) ? fullPath : null;
        }

        private LineEndingStyle DetectLineEndingStyle(string content)
        {
            var crlfCount = Regex.Matches(content, "\r\n").Count;
            var lfOnlyCount = Regex.Matches(content, "(?<!\r)\n").Count;
            
            if (crlfCount > 0 && lfOnlyCount == 0)
                return LineEndingStyle.CRLF;
            if (lfOnlyCount > 0 && crlfCount == 0)
                return LineEndingStyle.LF;
            if (crlfCount > 0 && lfOnlyCount > 0)
                return LineEndingStyle.Mixed;
                
            return LineEndingStyle.LF;
        }

        private string NormalizeLineEndings(string content)
        {
            return content?.Replace("\r\n", "\n") ?? string.Empty;
        }

        private string RestoreLineEndings(string content, LineEndingStyle style)
        {
            return style == LineEndingStyle.CRLF ? content.Replace("\n", "\r\n") : content;
        }

        private ReplacementResult ApplyStringReplacement(string content, string original, string replacement, int maxReplacements)
        {
            var positions = new List<int>();
            var modifiedContent = content;
            var replacementCount = 0;
            var searchFrom = 0;

            while (replacementCount < maxReplacements || maxReplacements == -1)
            {
                var index = modifiedContent.IndexOf(original, searchFrom, StringComparison.Ordinal);
                if (index == -1)
                    break;

                positions.Add(index);
                modifiedContent = modifiedContent.Substring(0, index) + replacement + modifiedContent.Substring(index + original.Length);
                replacementCount++;
                
                searchFrom = index + replacement.Length;
            }

            if (replacementCount == 0)
            {
                return ReplacementResult.CreateFailure($"Original content not found in file.");
            }

            return ReplacementResult.CreateSuccess(modifiedContent, replacementCount, positions);
        }

        private ReplacementResult ApplyRegexReplacement(string content, string pattern, string replacement, int maxReplacements)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.Multiline);
                var matches = regex.Matches(content);
                
                if (matches.Count == 0)
                {
                    return ReplacementResult.CreateFailure($"Regex pattern not found in file.");
                }

                var actualReplacements = maxReplacements == -1 ? matches.Count : Math.Min(maxReplacements, matches.Count);
                var positions = matches.Take(actualReplacements).Select(m => m.Index).ToList();
                
                var result = regex.Replace(content, replacement, actualReplacements);
                
                return ReplacementResult.CreateSuccess(result, actualReplacements, positions);
            }
            catch (Exception ex)
            {
                return ReplacementResult.CreateFailure($"Regex replacement failed: {ex.Message}");
            }
        }

        private string GeneratePreview(string original, string modified, List<int> positions)
        {
            var lines = original.Split('\n');
            var modifiedLines = modified.Split('\n');
            var preview = "Changes preview:\n";
            
            for (int i = 0; i < Math.Min(lines.Length, modifiedLines.Length) && i < 10; i++)
            {
                if (lines[i] != modifiedLines[i])
                {
                    preview += $"Line {i + 1}:\n";
                    preview += $"- {lines[i]}\n";
                    preview += $"+ {modifiedLines[i]}\n";
                }
            }
            
            return preview;
        }

        private string? CreateBackup(string filePath, string content)
        {
            try
            {
                var backupManager = new LogiqBackupManager();
                var backupId = backupManager.CreateBackupAsync(filePath, content, "ApplyDiffTool", "pre-modification", 
                    "Backup before applying diff").GetAwaiter().GetResult();
                return backupId;
            }
            catch
            {
                try
                {
                    var backupPath = $"{filePath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    File.WriteAllText(backupPath, content);
                    return backupPath;
                }
                catch
                {
                    return null;
                }
            }
        }

    }
}
