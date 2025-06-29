using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core;

namespace LogiQCLI.Commands.Backup
{
    [CommandMetadata("Backup", Tags = new[] { "backup", "safe", "query" }, Alias = "backup-status")]
    public class BackupStatusCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "backup-status",
                Description = "Show backup system status and statistics",
                Alias = "bstatus"
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var commandArgs = new { action = "status" };
                var json = JsonSerializer.Serialize(commandArgs);
                
                var backupTool = new BackupCommandsTool();
                return await backupTool.Execute(json);
            }
            catch (Exception ex)
            {
                return $"[red]Error showing backup status: {ex.Message}[/]";
            }
        }
    }
} 
