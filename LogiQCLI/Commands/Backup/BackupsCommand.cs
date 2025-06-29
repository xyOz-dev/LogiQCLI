using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Backup
{
    [CommandMetadata("Backup", Tags = new[] { "backup", "management" })]
    public class BackupsCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "backups",
                Description = "List workspace backups. Usage: /backups [file-path] [limit]",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        filePath = new
                        {
                            type = "string",
                            description = "Filter backups for specific file (optional)"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum number of backups to show (optional)"
                        }
                    }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {

                string? filePath = null;
                int? limit = null;

                if (!string.IsNullOrWhiteSpace(args))
                {
                    var parts = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        filePath = parts[0];
                    }
                    if (parts.Length > 1 && int.TryParse(parts[1], out var limitValue))
                    {
                        limit = limitValue;
                    }
                }

                var commandArgs = new { action = "list", filePath, limit };
                var json = JsonSerializer.Serialize(commandArgs);
                
                var backupTool = new BackupCommandsTool();
                return await backupTool.Execute(json);
            }
            catch (Exception ex)
            {
                return $"[red]Error executing backup command: {ex.Message}[/]";
            }
        }
    }
} 
