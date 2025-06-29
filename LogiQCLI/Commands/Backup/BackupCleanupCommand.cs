using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Backup
{
    [CommandMetadata("Backup", Tags = new[] { "backup", "maintenance", "cleanup" }, Alias = "backup-cleanup")]
    public class BackupCleanupCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "backup-cleanup",
                Description = "Clean up old backups based on retention policy. Usage: /backup-cleanup [retention-days]",
                Alias = "bcleanup",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        retentionDays = new
                        {
                            type = "integer",
                            description = "Number of days to retain backups (optional, defaults to workspace setting)"
                        }
                    }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                int? retentionDays = null;

                if (!string.IsNullOrWhiteSpace(args))
                {
                    var trimmedArgs = args.Trim();
                    if (int.TryParse(trimmedArgs, out var days))
                    {
                        retentionDays = days;
                    }
                    else
                    {
                        return "[yellow]Invalid retention days value. Please provide a valid integer.[/]";
                    }
                }

                var commandArgs = new { action = "cleanup", retentionDays };
                var json = JsonSerializer.Serialize(commandArgs);
                
                var backupTool = new BackupCommandsTool();
                return await backupTool.Execute(json);
            }
            catch (Exception ex)
            {
                return $"[red]Error cleaning up backups: {ex.Message}[/]";
            }
        }
    }
}
