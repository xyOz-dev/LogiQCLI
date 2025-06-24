using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.FileOperations.Arguments;

namespace LogiQCLI.Tools.FileOperations
{
    [ToolMetadata("FileOperations", Tags = new[] { "essential", "safe" })]
    public class ReadFileTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "read_file",
                Description = "Reads the entire contents of a text file and returns it as a string. " +
                              "Use this tool when you need to examine file contents before making changes, " +
                              "understand existing code structure, or verify file existence. " +
                              "Automatically handles path separators across platforms. " +
                              "For large files, consider using read_file_by_line_count for partial reads.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path (relative to workspace or absolute). " +
                                         "Supports both forward slashes and backslashes. " +
                                         "Examples: 'src/app.ts', './config.json', 'C:/projects/file.txt'"
                        }
                    },
                    Required = new[] { "path" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ReadFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path))
                {
                    return "Error: Invalid arguments. Path is required.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                return await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }

    }
}
