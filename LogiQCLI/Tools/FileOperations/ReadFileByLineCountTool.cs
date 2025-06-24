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
                Description = "Reads only the first N lines from a file, useful for previewing large files. " +
                              "Use this tool when you need to examine file headers, check file format, " +
                              "preview log files, or read configuration files without loading entire content. " +
                              "More efficient than read_file for large files when you only need the beginning.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace or absolute. " +
                                         "File must exist and be readable. " +
                                         "Examples: 'logs/app.log', 'data/large-dataset.csv'"
                        },
                        lineCount = new
                        {
                            type = "integer",
                            description = "Number of lines to read from the beginning. " +
                                         "Must be positive integer. " +
                                         "Example: 100 to read first 100 lines"
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
