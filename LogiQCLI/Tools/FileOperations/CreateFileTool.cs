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
    [ToolMetadata("FileOperations", Tags = new[] { "create", "write" })]
    public class CreateFileTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "create_file",
                Description = "Creates a new file with optional initial content, ensuring it doesn't accidentally overwrite existing files. " +
                              "Use this tool when you need to create new source files, configuration files, or documentation. " +
                              "Automatically creates any missing directories in the path. " +
                              "For modifying existing files, use write_file, apply_diff, or append_file instead.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace root. " +
                                         "Directories will be created if needed. " +
                                         "Examples: 'src/new-component.tsx', 'docs/README.md'"
                        },
                        content = new
                        {
                            type = "string",
                            description = "Initial file content. " +
                                         "Default: empty string (creates empty file). " +
                                         "Supports any text content including code, JSON, markdown."
                        },
                        overwrite = new
                        {
                            type = "boolean",
                            description = "Allow overwriting existing files. " +
                                         "Default: false (fails if file exists). " +
                                         "Set true only when intentionally replacing files."
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
                var arguments = JsonSerializer.Deserialize<CreateFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path))
                {
                    return "Error: Invalid arguments. Path is required.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));
                
                var overwrite = arguments.Overwrite ?? false;
                var fileExists = File.Exists(fullPath);
                
                if (fileExists && !overwrite)
                {
                    return $"Error: File already exists: {fullPath}. Set overwrite to true to replace it.";
                }

                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var content = arguments.Content ?? string.Empty;
                await File.WriteAllTextAsync(fullPath, content);
                
                var action = fileExists && overwrite ? "Overwrote" : "Created";
                return $"{action} file at {fullPath} with {content.Length} characters";
            }
            catch (Exception ex)
            {
                return $"Error creating file: {ex.Message}";
            }
        }
        
    }
}
