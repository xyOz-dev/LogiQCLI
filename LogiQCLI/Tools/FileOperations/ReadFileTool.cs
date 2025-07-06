using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.FileOperations.Arguments;
using LogiQCLI.Presentation.Console.Session;
using LogiQCLI.Core.Models.Configuration;

namespace LogiQCLI.Tools.FileOperations
{
    [ToolMetadata("FileOperations", Tags = new[] { "essential", "safe" })]
    public class ReadFileTool : ITool
    {
        private readonly FileReadRegistry _registry;
        private readonly ApplicationSettings _settings;

        public ReadFileTool(FileReadRegistry registry, ApplicationSettings settings)
        {
            _registry = registry;
            _settings = settings;
        }

        public ReadFileTool() : this(new FileReadRegistry(), new ApplicationSettings()) {}

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "read_file",
                Description = "Read contents of a file. Use for examining code, config files, or any text content before modifications.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace or absolute. Examples: 'src/app.ts', './config.json'"
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

                if (_settings.Experimental?.DeduplicateFileReads == true &&
                    _registry.TryGet(fullPath, out var meta))
                {
                    var info = new FileInfo(fullPath);
                    if (meta.LastWriteUtc == info.LastWriteTimeUtc && meta.Length == info.Length)
                    {
                        return "__UNCHANGED__";
                    }
                }

                return await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }

    }
}
