using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Backup
{
    [CommandMetadata("Backup", Tags = new[] { "backup", "safe", "query" }, Alias = "backup-diff")]
    public class BackupDiffCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "backup-diff",
                Description = "Show differences between a backup and current file. Usage: /backup-diff <backup-id> [compare-file-path]",
                Alias = "bdiff",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        backupId = new
                        {
                            type = "string",
                            description = "Backup ID to compare (required)"
                        },
                        comparePath = new
                        {
                            type = "string",
                            description = "File to compare against (optional)"
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
                return "[yellow]Usage: /backup-diff <backup-id> [compare-file-path][/]";
            }

            try
            {
                var parts = args.Trim().Split(' ', 2);
                var backupId = parts[0];
                var comparePath = parts.Length > 1 ? parts[1] : (string?)null;

                var commandArgs = new { action = "diff", backupId, filePath = comparePath };
                var json = JsonSerializer.Serialize(commandArgs);
                
                var backupTool = new BackupCommandsTool();
                return await backupTool.Execute(json);
            }
            catch (Exception ex)
            {
                return $"[red]Error showing backup diff: {ex.Message}[/]";
            }
        }
    }
} 
