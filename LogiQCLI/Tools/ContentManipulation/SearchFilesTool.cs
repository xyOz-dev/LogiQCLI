using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.ContentManipulation.Objects;
using LogiQCLI.Tools.ContentManipulation.Arguments;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Tools.ContentManipulation
{
    [ToolMetadata("ContentManipulation", Tags = new[] { "essential", "safe", "query" })]
    public class SearchFilesTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "search_files",
                Description = "Search for text patterns across multiple files using regex or plain text. " +
                              "Returns matching lines with context and file locations. " +
                              "Useful for finding code patterns, TODO comments, or specific implementations. " +
                              "Supports filtering by file patterns and case sensitivity.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        pattern = new
                        {
                            type = "string",
                            description = "Search pattern (plain text or regex). " +
                                         "Examples: 'TODO:', 'class\\s+\\w+Manager', 'import.*OpenRouter'"
                        },
                        path = new
                        {
                            type = "string",
                            description = "Directory to search in, relative to workspace. " +
                                         "Default: '.' (current directory). " +
                                         "Examples: 'src', 'OpenRouter', '../external'"
                        },
                        file_pattern = new
                        {
                            type = "string",
                            description = "File name pattern to filter (supports wildcards). " +
                                         "Default: '*' (all files). " +
                                         "Examples: '*.cs', '*.json', 'Test*.cs'"
                        },
                        use_regex = new
                        {
                            type = "boolean",
                            description = "Treat pattern as regex. " +
                                         "Default: false (plain text search)."
                        },
                        case_sensitive = new
                        {
                            type = "boolean",
                            description = "Case-sensitive search. " +
                                         "Default: true."
                        },
                        max_results = new
                        {
                            type = "integer",
                            description = "Maximum number of results to return. " +
                                         "Default: 50. Use -1 for unlimited."
                        }
                    },
                    Required = new[] { "pattern" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<SearchFilesArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Pattern))
                {
                    return "Error: Invalid arguments. Pattern is required.";
                }

                var searchPath = string.IsNullOrEmpty(arguments.Path) ? "." : arguments.Path;
                var cleanPath = searchPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(cleanPath);

                if (!Directory.Exists(fullPath))
                {
                    return $"Error: Directory does not exist: {fullPath}";
                }

                var filePattern = string.IsNullOrEmpty(arguments.FilePattern) ? "*" : arguments.FilePattern;
                var useRegex = arguments.UseRegex ?? false;
                var caseSensitive = arguments.CaseSensitive ?? true;
                var maxResults = arguments.MaxResults ?? 50;

                var results = new List<string>();
                var files = Directory.GetFiles(fullPath, filePattern, SearchOption.AllDirectories);
                var workspaceRoot = Directory.GetCurrentDirectory();
                var resultCount = 0;

                foreach (var file in files)
                {
                    if (resultCount >= maxResults && maxResults != -1)
                        break;

                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var lines = content.Split('\n');
                        var relativePath = Path.GetRelativePath(workspaceRoot, file);

                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (resultCount >= maxResults && maxResults != -1)
                                break;

                            bool isMatch = false;
                            
                            if (useRegex)
                            {
                                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                                isMatch = Regex.IsMatch(lines[i], arguments.Pattern, options);
                            }
                            else
                            {
                                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                                isMatch = lines[i].Contains(arguments.Pattern, comparison);
                            }

                            if (isMatch)
                            {
                                results.Add($"{relativePath}:{i + 1}: {lines[i].Trim()}");
                                resultCount++;
                            }
                        }
                    }
                    catch
                    {

                    }
                }

                if (results.Count == 0)
                {
                    return $"No matches found for pattern '{arguments.Pattern}' in {fullPath}";
                }

                var output = new StringBuilder();
                output.AppendLine($"Found {results.Count} matches for '{arguments.Pattern}':");
                output.AppendLine();
                
                foreach (var result in results)
                {
                    output.AppendLine(result);
                }

                if (resultCount >= maxResults && maxResults != -1)
                {
                    output.AppendLine();
                    output.AppendLine($"(Limited to first {maxResults} results)");
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error searching files: {ex.Message}";
            }
        }

    }
}
