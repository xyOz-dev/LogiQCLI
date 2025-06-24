using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.SystemOperations.Objects;

namespace LogiQCLI.Tools.SystemOperations
{
    [ToolMetadata("SystemOperations", Tags = new[] { "essential", "safe", "query" })]
    public class ListFilesTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_files",
                Description = "Lists files and directories at a specified location to explore project structure. " +
                              "Use this tool to discover available files, check if files exist, " +
                              "understand directory organization, or find files matching patterns. " +
                              "Returns paths relative to the workspace root for easy use with other tools.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "Directory path relative to workspace root. " +
                                         "Use '.' for current workspace directory. " +
                                         "Examples: '.', 'src', 'src/components', '../external-dir'"
                        },
                        recursive = new
                        {
                            type = "boolean",
                            description = "Include all subdirectories and their contents. " +
                                         "Default: false (only immediate children). " +
                                         "Set true to see full directory tree structure."
                        }
                    },
                    Required = new[] { "path" }
                }
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ListFilesArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path))
                {
                    return Task.FromResult("Error: Invalid arguments. Path is required.");
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                if (!Directory.Exists(fullPath))
                {
                    return Task.FromResult($"Error: Directory does not exist: {fullPath}");
                }

                var recursive = arguments.Recursive ?? false;
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var entries = Directory.EnumerateFileSystemEntries(fullPath, "*", searchOption);
                var workspaceRoot = Directory.GetCurrentDirectory();
                var relativeEntries = entries.Select(e => Path.GetRelativePath(workspaceRoot, e));

                return Task.FromResult(string.Join("\n", relativeEntries));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Error executing list_files tool: {ex.Message}");
            }
        }
    }
}
