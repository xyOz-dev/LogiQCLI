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
    public class AppendFileTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "append_file",
                Description = "Add content to end of existing file without overwriting. Creates file if it doesn't exist. Automatically handles newlines for proper formatting.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace. Creates file if it doesn't exist."
                        },
                        content = new
                        {
                            type = "string",
                            description = "Content to add at file end. Existing content is preserved."
                        },
                        newline = new
                        {
                            type = "boolean",
                            description = "Add newline before appending to separate from existing content. Default: true."
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
                var arguments = JsonSerializer.Deserialize<AppendFileArguments>(args);
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

                var content = arguments.Content ?? string.Empty;
                var addNewline = arguments.Newline ?? true;
                
                if (File.Exists(fullPath) && addNewline && new FileInfo(fullPath).Length > 0)
                {
                    content = Environment.NewLine + content;
                }

                await File.AppendAllTextAsync(fullPath, content);
                return $"Successfully appended {content.Length} characters to {fullPath}";
            }
            catch (Exception ex)
            {
                return $"Error appending to file: {ex.Message}";
            }
        }

    }
}
