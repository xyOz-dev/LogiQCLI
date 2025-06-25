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
                Description = "List files and directories to explore project structure. Returns paths relative to workspace root for easy use with other tools.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "Directory path relative to workspace. Default: '.' (workspace root)."
                        },
                        recursive = new
                        {
                            type = "boolean",
                            description = "Include all subdirectories and their contents. Default: false."
                        }
                    },
                    Required = Array.Empty<string>()
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
