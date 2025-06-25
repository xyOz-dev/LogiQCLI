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
    [ToolMetadata("FileOperations", Tags = new[] { "destructive" })]
    public class DeleteFileTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "delete_file",
                Description = "Permanently delete files or directories. IRREVERSIBLE operation. Has built-in protection for critical files (.git, package.json, etc).",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File or directory path relative to workspace. Must exist."
                        },
                        recursive = new
                        {
                            type = "boolean",
                            description = "For directories: delete all contents including subdirectories. Default: false (empty dirs only)."
                        },
                        force = new
                        {
                            type = "boolean",
                            description = "Bypass protection for critical files. Default: false. Use with extreme caution."
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
                var arguments = JsonSerializer.Deserialize<DeleteFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path))
                {
                    return "Error: Invalid arguments. Path is required.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    return $"Error: Path does not exist: {fullPath}";
                }

                var isDirectory = Directory.Exists(fullPath);
                var force = arguments.Force ?? false;
                var recursive = arguments.Recursive ?? false;

                if (!force && IsCriticalPath(fullPath))
                {
                    return $"Error: Cannot delete critical path: {fullPath}. Set force to true to override.";
                }

                if (isDirectory)
                {
                    if (recursive)
                    {
                        Directory.Delete(fullPath, true);
                        return $"Successfully deleted directory and all contents: {fullPath}";
                    }
                    else
                    {
                        if (Directory.GetFileSystemEntries(fullPath).Length > 0)
                        {
                            return $"Error: Directory is not empty: {fullPath}. Set recursive to true to delete contents.";
                        }
                        Directory.Delete(fullPath);
                        return $"Successfully deleted empty directory: {fullPath}";
                    }
                }
                else
                {
                    var fileInfo = new FileInfo(fullPath);
                    var fileSize = fileInfo.Length;
                    File.Delete(fullPath);
                    return $"Successfully deleted file: {fullPath} ({FormatFileSize(fileSize)})";
                }
            }
            catch (Exception ex)
            {
                return $"Error deleting path: {ex.Message}";
            }
        }

        private bool IsCriticalPath(string path)
        {
            var criticalPaths = new[] { ".git", ".gitignore", "package.json", "tsconfig.json", "appsettings.json" };
            var fileName = Path.GetFileName(path).ToLowerInvariant();
            return Array.Exists(criticalPaths, critical => critical.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
