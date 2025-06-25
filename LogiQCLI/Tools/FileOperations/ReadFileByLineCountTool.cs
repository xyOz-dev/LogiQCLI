using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.FileOperations.Arguments;

namespace LogiQCLI.Tools.FileOperations
{
    [ToolMetadata("FileOperations", Tags = new[] { "safe", "query" })]
    public class ReadFileByLineCountTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "read_file_by_line_count",
                Description = "Read only the first N lines from files. More efficient than read_file for large files when you only need the beginning portion.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace or absolute. File must exist and be readable."
                        },
                        lineCount = new
                        {
                            type = "integer",
                            description = "Number of lines to read from beginning. Must be positive integer."
                        }
                    },
                    Required = new[] { "path", "lineCount" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ReadFileByLineCountArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path) || arguments.LineCount <= 0)
                {
                    return "Error: Invalid arguments. Path is required and line count must be positive.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                var lines = await File.ReadAllLinesAsync(fullPath);
                return string.Join("\n", lines.Take(arguments.LineCount));
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }

    }
}
