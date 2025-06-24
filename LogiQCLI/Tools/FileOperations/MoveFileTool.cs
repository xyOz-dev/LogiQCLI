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
    public class MoveFileTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "move_file",
                Description = "Moves or renames files and directories to new locations. " +
                              "Use this tool for reorganizing project structure, renaming files, " +
                              "moving files between folders, or relocating entire directories. " +
                              "Intelligently handles destination paths - if destination is a directory, " +
                              "moves the source into it; if it's a filename, renames to that name.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        source = new
                        {
                            type = "string",
                            description = "Source file or directory path relative to workspace. " +
                                         "Must exist or operation will fail. " +
                                         "Examples: 'old-name.txt', 'src/utils/', 'temp/draft.md'"
                        },
                        destination = new
                        {
                            type = "string",
                            description = "Destination path (new name or target directory). " +
                                         "For renaming: 'new-name.txt'. " +
                                         "For moving: 'target-folder/' or 'target-folder/new-name.txt'"
                        },
                        overwrite = new
                        {
                            type = "boolean",
                            description = "Replace existing files at destination. " +
                                         "Default: false (fails if destination exists). " +
                                         "For directories, merges into existing directory when true."
                        },
                        createDirectory = new
                        {
                            type = "boolean",
                            description = "Create missing parent directories in destination path. " +
                                         "Default: true (auto-creates directory structure). " +
                                         "Set false to ensure destination directory already exists."
                        }
                    },
                    Required = new[] { "source", "destination" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<MoveFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Source) || string.IsNullOrEmpty(arguments.Destination))
                {
                    return "Error: Invalid arguments. Source and destination are required.";
                }

                var sourcePath = Path.GetFullPath(arguments.Source.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));
                    
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    return $"Error: Source does not exist: {sourcePath}";
                }
                
                var destPath = Path.GetFullPath(arguments.Destination.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                var isDirectory = Directory.Exists(sourcePath);
                var overwrite = arguments.Overwrite ?? false;
                var createDir = arguments.CreateDirectory ?? true;

                if (isDirectory)
                {
                    return MoveDirectory(sourcePath, destPath, overwrite, createDir);
                }
                else
                {
                    return MoveFile(sourcePath, destPath, overwrite, createDir);
                }
            }
            catch (Exception ex)
            {
                return $"Error moving file/directory: {ex.Message}";
            }
        }

        private string MoveFile(string sourcePath, string destPath, bool overwrite, bool createDir)
        {
            var destDir = Path.GetDirectoryName(destPath);
            
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                if (createDir)
                {
                    Directory.CreateDirectory(destDir);
                }
                else
                {
                    return $"Error: Destination directory does not exist: {destDir}";
                }
            }

            if (Directory.Exists(destPath))
            {
                destPath = Path.Combine(destPath, Path.GetFileName(sourcePath));
            }

            if (File.Exists(destPath) && !overwrite)
            {
                return $"Error: Destination file already exists: {destPath}. Set overwrite to true to replace it.";
            }

            File.Move(sourcePath, destPath, overwrite);
            return $"Successfully moved file from {sourcePath} to {destPath}";
        }

        private string MoveDirectory(string sourcePath, string destPath, bool overwrite, bool createDir)
        {
            if (Directory.Exists(destPath))
            {
                if (!overwrite)
                {
                    return $"Error: Destination directory already exists: {destPath}. Set overwrite to true to merge.";
                }
                
                destPath = Path.Combine(destPath, Path.GetFileName(sourcePath));
            }

            var destParent = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destParent) && !Directory.Exists(destParent))
            {
                if (createDir)
                {
                    Directory.CreateDirectory(destParent);
                }
                else
                {
                    return $"Error: Parent directory does not exist: {destParent}";
                }
            }

            Directory.Move(sourcePath, destPath);
            return $"Successfully moved directory from {sourcePath} to {destPath}";
        }
    }
}
