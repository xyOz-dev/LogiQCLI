using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Backup
{
    [CommandMetadata("Backup", Tags = new[] { "backup", "destructive" })]
    public class RestoreCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "restore",
                Description = "Restore a backup by ID. Usage: /restore <backup-id> [target-path]",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        backupId = new
                        {
                            type = "string",
                            description = "Backup ID to restore (required)"
                        },
                        targetPath = new
                        {
                            type = "string",
                            description = "Target path for restoration (optional)"
                        }
                    },
                    Required = new[] { "backupId" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return "[yellow]Usage: /restore <backup-id> [target-path][/]";
            }

            try
            {
                var parts = args.Trim().Split(' ', 2);
                var backupId = parts[0];
                var targetPath = parts.Length > 1 ? parts[1] : (string?)null;

                var commandArgs = new { action = "restore", backupId, filePath = targetPath };
                var json = JsonSerializer.Serialize(commandArgs);
                
                var backupTool = new BackupCommandsTool();
                return await backupTool.Execute(json);
            }
            catch (Exception ex)
            {
                return $"[red]Error restoring backup: {ex.Message}[/]";
            }
        }
    }
} 
