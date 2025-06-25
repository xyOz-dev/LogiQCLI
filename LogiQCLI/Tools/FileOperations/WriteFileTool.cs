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
    [ToolMetadata("FileOperations", Tags = new[] { "write" })]
    public class WriteFileTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "write_file",
                Description = "Create new file or completely replace existing file content. Overwrites any existing content.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace. Auto-creates parent directories if needed."
                        },
                        content = new
                        {
                            type = "string",
                            description = "Complete file content to write. Use empty string for blank file."
                        }
                    },
                    Required = new[] { "path", "content" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<WriteFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path))
                {
                    return "Error: Invalid arguments. Path is required.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));
                    
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(fullPath, arguments.Content ?? string.Empty);
                return $"Successfully wrote {arguments.Content?.Length ?? 0} characters to {fullPath}";
            }
            catch (Exception ex)
            {
                return $"Error writing file: {ex.Message}";
            }
        }
    }
}
