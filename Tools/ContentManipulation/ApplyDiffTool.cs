using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.ContentManipulation.Objects;
using LogiQCLI.Tools.Core.Interfaces;

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
                Description = "Applies precise, targeted modifications to files by finding and replacing specific content blocks. " +
                              "Use this tool for surgical edits when you know the exact content to change, such as updating function implementations, " +
                              "modifying configuration values, or replacing code blocks. Preserves file formatting and line endings. " +
                              "Supports encoding and JSON output for automation. Maximum file size: 10MB. For simple find/replace operations, consider search_and_replace.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new { type = "string", description = "File path relative to workspace root. File must exist and be under 10MB. Example: 'src/components/Header.tsx'" },
                        original = new { type = "string", description = "Exact content to search for, including whitespace and line breaks. Must match precisely unless useRegex is true." },
                        replacement = new { type = "string", description = "New content to replace the original with. Can be empty string to delete content." },
                        maxReplacements = new { type = "number", description = "Maximum replacements to perform. Default: 1. Use -1 for all occurrences." },
                        useRegex = new { type = "boolean", description = "Interpret 'original' as a regex pattern. Default: false." },
                        createBackup = new { type = "boolean", description = "Create a timestamped backup before writing. Default: true." },
                        preview = new { type = "boolean", description = "Show changes without applying them. Default: false." },
                        encoding = new { type = "string", description = "File encoding (utf-8, utf-16, ascii, etc). Default: utf-8." },
                        rawOutput = new { type = "boolean", description = "If true, return output as plain text instead of JSON. Default: false." },
                        previewLines = new { type = "number", description = "Number of previewed lines if preview=true. Default: 10." }
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
                var validationError = ValidateArguments(arguments);
                if (validationError != null)
                {
                    return OutputResult(arguments, false, validationError, 0, null, null, null, null, null);
                }

                var fullPath = GetValidFilePath(arguments.Path);
                if (fullPath == null)
                {
                    return OutputResult(arguments, false, $"Error: File not found: {arguments.Path}. Current directory: {Directory.GetCurrentDirectory()}", 0, null, null, null, null, null);
                }

                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    return OutputResult(arguments, false, $"Error: File too large ({fileInfo.Length:N0} bytes). Maximum supported size is {MaxFileSizeBytes:N0} bytes.", 0, null, null, null, null, null);
                }

                Encoding fileEncoding = ParseEncoding(arguments.Encoding);
                string originalContent;
                using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(fileStream, fileEncoding, true))
                {
                    originalContent = await reader.ReadToEndAsync();
                }

                var lineEndingStyle = DetectLineEndingStyle(originalContent);
                var normalizedContent = NormalizeLineEndings(originalContent);
                var normalizedOriginal = NormalizeLineEndings(arguments.Original);
                var normalizedReplacement = NormalizeLineEndings(arguments.Replacement);

                var result = arguments.UseRegex
                    ? ApplyRegexReplacement(normalizedContent, normalizedOriginal, normalizedReplacement, arguments.MaxReplacements)
                    : ApplyStringReplacement(normalizedContent, normalizedOriginal, normalizedReplacement, arguments.MaxReplacements);

                if (!result.Success)
                {
                    return OutputResult(arguments, false, $"Error: {result.ErrorMessage}", 0, null, null, originalContent.Substring(0, Math.Min(200, originalContent.Length)), null, null);
                }

                if (arguments.Preview)
                {
                    return OutputResult(arguments, true, $"Preview mode - would make {result.ReplacementCount} replacement(s)", result.ReplacementCount, null, null, null,
                        GeneratePreview(normalizedContent, result.ModifiedContent, result.MatchPositions, arguments.PreviewLines));
                }

                var finalContent = RestoreLineEndings(result.ModifiedContent, lineEndingStyle);

                string backupPath = null;
                if (arguments.CreateBackup)
                {
                    backupPath = CreateBackup(fullPath, originalContent, fileEncoding);
                }

                try
                {
                    using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fileStream, fileEncoding))
                    {
                        await writer.WriteAsync(finalContent);
                    }

                    return OutputResult(arguments, true, $"Successfully applied diff to {fullPath}.", result.ReplacementCount, backupPath, null, null, null);
                }
                catch (Exception writeEx)
                {
                    if (backupPath != null)
                    {
                        try
                        {
                            File.Copy(backupPath, fullPath, true);
                            return OutputResult(arguments, false, $"Error writing file: {writeEx.Message}. File restored from backup.", 0, backupPath, null, null, null);
                        }
                        catch
                        {
                            return OutputResult(arguments, false, $"Error writing file: {writeEx.Message}. WARNING: Could not restore from backup at {backupPath}", 0, backupPath, null, null, null);
                        }
                    }
                    return OutputResult(arguments, false, $"Error writing file: {writeEx.Message}", 0, null, null, null, null);
                }
            }
            catch (Exception ex)
            {
                return OutputResult(null, false, $"Error applying diff: {ex.Message}", 0, null, null, null, null);
            }
        }

        private string OutputResult(
            ApplyDiffArguments args,
            bool success,
            string message,
            int replacements,
            string backupPath = null,
            string error = null,
            string filePreview = null,
            string previewDiff = null,
            object extra = null)
        {
            if (args != null && args.RawOutput)
            {
                var plainMsg = success
                    ? $"SUCCESS: {message} Replacements: {replacements}." + (backupPath != null ? $" Backup: {backupPath}." : "")
                    : $"FAILED: {message} {error}";
                if (args.Preview && previewDiff != null)
                {
                    plainMsg += "\n" + previewDiff;
                }
                if (filePreview != null) plainMsg += "\nFile Preview: " + filePreview;
                return plainMsg;
            }
            var obj = new
            {
                success,
                message,
                replacements,
                backupPath,
                error,
                filePreview,
                previewDiff,
                extra
            };
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }

        private string ValidateArguments(ApplyDiffArguments arguments)
        {
            if (arguments == null)
                return "Invalid arguments provided.";
            if (string.IsNullOrEmpty(arguments.Path))
                return "Path is required.";
            if (arguments.Original == null)
                return "Original content cannot be null.";
            if (arguments.Replacement == null)
                return "Replacement content cannot be null.";
            if (string.IsNullOrEmpty(arguments.Original))
                return "Original content cannot be empty (use empty string for replacement).";
            if (arguments.MaxReplacements == 0)
                return "MaxReplacements cannot be 0. Use -1 for all replacements or a positive number.";
            if (arguments.PreviewLines < 1)
                return "previewLines must be a positive integer.";
            if (arguments.UseRegex)
            {
                try { new Regex(arguments.Original); }
                catch (ArgumentException ex) { return $"Invalid regex pattern in original: {ex.Message}"; }
            }
            try { ParseEncoding(arguments.Encoding); }
            catch (Exception ex) { return $"Invalid encoding: {ex.Message}"; }
            return null;
        }

        private string GetValidFilePath(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return null;
            var fullPath = Path.GetFullPath(inputPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            return File.Exists(fullPath) ? fullPath : null;
        }
        
        private Encoding ParseEncoding(string encoding)
        {
            if (string.IsNullOrWhiteSpace(encoding)) return Encoding.UTF8;
            switch (encoding.ToLower())
            {
                case "utf-8": return Encoding.UTF8;
                case "utf-16": return Encoding.Unicode;
                case "ascii": return Encoding.ASCII;
                case "latin1": return Encoding.GetEncoding("ISO-8859-1");
                default: return Encoding.GetEncoding(encoding);
            }
        }

        private LineEndingStyle DetectLineEndingStyle(string content)
        {
            var crlfCount = Regex.Matches(content, "\r\n").Count;
            var lfOnlyCount = Regex.Matches(content, "(?<!\r)\n").Count;
            if (crlfCount > 0 && lfOnlyCount == 0) return LineEndingStyle.CRLF;
            if (lfOnlyCount > 0 && crlfCount == 0) return LineEndingStyle.LF;
            if (crlfCount > 0 && lfOnlyCount > 0) return LineEndingStyle.Mixed;
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
                if (index == -1) break;
                positions.Add(index);
                modifiedContent = modifiedContent.Substring(0, index) + replacement + modifiedContent.Substring(index + original.Length);
                replacementCount++;
                searchFrom = index + replacement.Length;
            }
            if (replacementCount == 0)
            {
                return ReplacementResult.CreateFailure("Original content not found in file.");
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
                    return ReplacementResult.CreateFailure("Regex pattern not found in file.");
                }
                var actualReplacements = maxReplacements == -1 ? matches.Count : Math.Min(maxReplacements, matches.Count);
                var positions = matches.Cast<Match>().Take(actualReplacements).Select(m => m.Index).ToList();
                var result = regex.Replace(content, replacement, actualReplacements);
                return ReplacementResult.CreateSuccess(result, actualReplacements, positions);
            }
            catch (Exception ex)
            {
                return ReplacementResult.CreateFailure($"Regex replacement failed: {ex.Message}");
            }
        }

        private string GeneratePreview(string original, string modified, List<int> positions, int previewLines)
        {
            var origLines = original.Split('\n');
            var modLines = modified.Split('\n');
            var changes = new List<string>();
            int shown = 0;
            for (int i = 0; i < Math.Min(origLines.Length, modLines.Length); i++)
            {
                if (origLines[i] != modLines[i])
                {
                    // Add a few context lines before/after change
                    var firstCtx = Math.Max(0, i - 2);
                    var lastCtx = Math.Min(origLines.Length - 1, i + 2);
                    for (int ctx = firstCtx; ctx <= lastCtx; ctx++)
                    {
                        if (ctx == i)
                        {
                            changes.Add($"- {origLines[ctx]}");
                            changes.Add($"+ {modLines[ctx]}");
                        }
                        else
                        {
                            changes.Add($"  {origLines[ctx]}");
                        }
                    }
                    changes.Add("");
                    shown++;
                    if (shown >= previewLines) break;
                }
            }
            return "Changes preview:\n" + string.Join("\n", changes);
        }

        private string CreateBackup(string filePath, string content, Encoding encoding)
        {
            try
            {
                var backupPath = $"{filePath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                File.WriteAllText(backupPath, content, encoding);
                return backupPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
