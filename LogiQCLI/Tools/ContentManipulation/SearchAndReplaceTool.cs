using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.ContentManipulation.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core;

namespace LogiQCLI.Tools.ContentManipulation
{
    [ToolMetadata("ContentManipulation", Tags = new[] { "write" })]
    public class SearchAndReplaceTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "search_and_replace",
                Description = "Global search and replace operations across entire files. Replaces ALL occurrences. Supports regex patterns.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace. File must exist."
                        },
                        search = new
                        {
                            type = "string",
                            description = "Text or regex pattern to find. Example: 'oldVariable' or '\\bversion\\s*=\\s*[\"\\']([^\"\\']+)[\"\\']'"
                        },
                        replace = new
                        {
                            type = "string",
                            description = "Replacement text. Use regex capture groups like $1 when useRegex=true. Empty string deletes matches."
                        },
                        useRegex = new
                        {
                            type = "boolean",
                            description = "Treat search as regex pattern. Default: false. Enables capture groups and pattern matching."
                        },
                        caseSensitive = new
                        {
                            type = "boolean",
                            description = "Match case exactly. Default: true."
                        },
                        multiline = new
                        {
                            type = "boolean",
                            description = "In regex mode: ^ and $ match line boundaries. Default: true. Only applies when useRegex=true."
                        },
                        backup = new
                        {
                            type = "boolean",
                            description = "Create a backup entry in the .logiq-backups folder before modifying. Default: true."
                        },
                        dryRun = new
                        {
                            type = "boolean",
                            description = "Preview changes without modifying the file. Shows what would be replaced. Default: false."
                        },
                        dotAll = new
                        {
                            type = "boolean",
                            description = "In regex mode: . matches any character including newlines. Useful for multi-line patterns. Default: false."
                        },
                        showProgress = new
                        {
                            type = "boolean",
                            description = "Show progress information for large files. Default: false."
                        }
                    },
                    Required = new[] { "path", "search", "replace" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<SearchAndReplaceArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path) || arguments.Search == null)
                {
                    return "Error: Invalid arguments. Path and search are required.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                var fileInfo = new FileInfo(fullPath);
                
                var content = await File.ReadAllTextAsync(fullPath);
                var originalContent = content;
                
                var useRegex = arguments.UseRegex ?? false;
                var caseSensitive = arguments.CaseSensitive ?? true;
                var multiline = arguments.Multiline ?? true;
                var backup = arguments.Backup ?? true;
                var dryRun = arguments.DryRun ?? false;
                var dotAll = arguments.DotAll ?? false;
                var replaceText = arguments.Replace ?? string.Empty;

                int replacementCount;
                string newContent;

                if (useRegex)
                {
                    var options = RegexOptions.None;
                    if (!caseSensitive) options |= RegexOptions.IgnoreCase;
                    if (multiline) options |= RegexOptions.Multiline;
                    if (dotAll) options |= RegexOptions.Singleline;

                    var regex = new Regex(arguments.Search, options);
                    var matches = regex.Matches(content);
                    replacementCount = matches.Count;
                    newContent = regex.Replace(content, replaceText);
                }
                else
                {
                    var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    replacementCount = CountOccurrences(content, arguments.Search, comparison);
                    newContent = ReplaceAll(content, arguments.Search, replaceText, comparison);
                }

                if (replacementCount == 0)
                {
                    return $"No matches found for '{arguments.Search}' in {fullPath}";
                }

                if (dryRun)
                {
                    var preview = GeneratePreview(originalContent, newContent, arguments.Search, replaceText, 
                        useRegex, caseSensitive, multiline, dotAll);
                    return $"Preview mode - would make {replacementCount} replacement(s):\n{preview}";
                }

                if (backup && originalContent != newContent)
                {
                    try
                    {
                        var backupManager = new LogiqBackupManager();
                        await backupManager.CreateBackupAsync(fullPath, originalContent, "SearchAndReplaceTool", "pre-modification",
                            "Backup before search and replace");
                    }
                    catch
                    {
                    }
                }

                await File.WriteAllTextAsync(fullPath, newContent);

                var result = $"Successfully replaced {replacementCount} occurrence(s) in {fullPath}";
                
                if ((arguments.ShowProgress ?? false) || fileInfo.Length > 1024 * 1024)
                {
                    result += $"\nFile size: {FormatFileSize(fileInfo.Length)}";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return $"Error during search and replace: {ex.Message}";
            }
        }

        private int CountOccurrences(string text, string pattern, StringComparison comparison)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, comparison)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        private string ReplaceAll(string text, string oldValue, string newValue, StringComparison comparison)
        {
            if (comparison == StringComparison.Ordinal)
            {
                return text.Replace(oldValue, newValue);
            }

            var result = text;
            int index = 0;
            while ((index = result.IndexOf(oldValue, index, comparison)) != -1)
            {
                result = result.Remove(index, oldValue.Length).Insert(index, newValue);
                index += newValue.Length;
            }
            return result;
        }


        private string GeneratePreview(string original, string modified, string search, string replace, 
            bool useRegex, bool caseSensitive, bool multiline, bool dotAll)
        {
            var originalLines = original.Split('\n');
            var modifiedLines = modified.Split('\n');
            var preview = new System.Text.StringBuilder();
            preview.AppendLine("Changes preview:");
            
            int changesShown = 0;
            const int maxChanges = 10;
            
            for (int i = 0; i < Math.Min(originalLines.Length, modifiedLines.Length); i++)
            {
                if (originalLines[i] != modifiedLines[i])
                {
                    preview.AppendLine($"Line {i + 1}:");
                    preview.AppendLine($"- {originalLines[i].TrimEnd('\r')}");
                    preview.AppendLine($"+ {modifiedLines[i].TrimEnd('\r')}");
                    changesShown++;
                    
                    if (changesShown >= maxChanges)
                    {
                        var remainingChanges = CountRemainingChanges(originalLines, modifiedLines, i + 1);
                        if (remainingChanges > 0)
                        {
                            preview.AppendLine($"... and {remainingChanges} more changes");
                        }
                        break;
                    }
                }
            }
            
            return preview.ToString();
        }
        
        private int CountRemainingChanges(string[] originalLines, string[] modifiedLines, int startIndex)
        {
            int count = 0;
            for (int i = startIndex; i < Math.Min(originalLines.Length, modifiedLines.Length); i++)
            {
                if (originalLines[i] != modifiedLines[i])
                {
                    count++;
                }
            }
            return count;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double fileSize = bytes;
            int index = 0;
            
            while (fileSize >= 1024 && index < sizes.Length - 1)
            {
                fileSize /= 1024;
                index++;
            }
            
            return $"{fileSize:0.##} {sizes[index]}";
        }

    }
}
