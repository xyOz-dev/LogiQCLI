using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Tools.Core
{
    [ToolMetadata("Core", Tags = new[] { "backup", "management", "essential" })]
    public class BackupCommandsTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "backup_commands",
                Description = "Manage workspace backups in .logiq system. List, restore, diff, and cleanup backups with human-readable timestamps.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        action = new
                        {
                            type = "string",
                            description = "Action to perform: 'list', 'restore', 'diff', 'cleanup', 'status'"
                        },
                        backupId = new
                        {
                            type = "string",
                            description = "Backup ID for restore/diff operations. Format: yyyyMMdd-HHmmss-xxxxxxxx"
                        },
                        filePath = new
                        {
                            type = "string",
                            description = "Filter backups by file path for list action, or target path for restore"
                        },
                        limit = new
                        {
                            type = "number",
                            description = "Maximum number of backups to show. Default: 20"
                        },
                        retentionDays = new
                        {
                            type = "number",
                            description = "Days to keep backups for cleanup action. Default: 30"
                        }
                    },
                    Required = new[] { "action" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var arguments = JsonSerializer.Deserialize<BackupCommandArguments>(args, options);
                if (arguments == null || string.IsNullOrEmpty(arguments.Action))
                {
                    return "Error: Action is required. Use: list, restore, diff, cleanup, or status";
                }

                var backupManager = new LogiqBackupManager();

                return arguments.Action.ToLower() switch
                {
                    "list" => await HandleListAction(backupManager, arguments),
                    "restore" => await HandleRestoreAction(backupManager, arguments),
                    "diff" => HandleDiffAction(backupManager, arguments),
                    "cleanup" => HandleCleanupAction(backupManager, arguments),
                    "status" => HandleStatusAction(backupManager),
                    _ => "Error: Invalid action. Use: list, restore, diff, cleanup, or status"
                };
            }
            catch (Exception ex)
            {
                return $"Error executing backup command: {ex.Message}";
            }
        }

        private async Task<string> HandleListAction(LogiqBackupManager backupManager, BackupCommandArguments arguments)
        {
            var backups = backupManager.ListBackups(arguments.FilePath, arguments.Limit ?? 20);
            
            if (!backups.Any())
            {
                return string.IsNullOrEmpty(arguments.FilePath) 
                    ? "No backups found in workspace."
                    : $"No backups found for file: {arguments.FilePath}";
            }

            var result = new StringBuilder();
            result.AppendLine($"Found {backups.Count} backup(s):");
            result.AppendLine();

            foreach (var backup in backups)
            {
                var timeAgo = GetTimeAgo(backup.Timestamp);
                var size = FormatFileSize(backup.FileSize);
                
                result.AppendLine($"ID: {backup.Id}");
                result.AppendLine($"  File: {backup.OriginalPath}");
                result.AppendLine($"  Time: {backup.Timestamp:yyyy-MM-dd HH:mm:ss} UTC ({timeAgo})");
                result.AppendLine($"  Tool: {backup.Tool}");
                result.AppendLine($"  Operation: {backup.Operation}");
                result.AppendLine($"  Size: {size}");
                
                if (!string.IsNullOrEmpty(backup.Description))
                {
                    result.AppendLine($"  Description: {backup.Description}");
                }
                
                result.AppendLine();
            }

            return result.ToString();
        }

        private async Task<string> HandleRestoreAction(LogiqBackupManager backupManager, BackupCommandArguments arguments)
        {
            if (string.IsNullOrEmpty(arguments.BackupId))
            {
                return "Error: BackupId is required for restore action";
            }

            var success = await backupManager.RestoreBackupAsync(arguments.BackupId, arguments.FilePath);
            if (success)
            {
                var targetPath = arguments.FilePath ?? "original location";
                return $"Successfully restored backup {arguments.BackupId} to {targetPath}";
            }
            else
            {
                return $"Failed to restore backup {arguments.BackupId}. Check if backup exists and target is writable.";
            }
        }

        private string HandleDiffAction(LogiqBackupManager backupManager, BackupCommandArguments arguments)
        {
            if (string.IsNullOrEmpty(arguments.BackupId))
            {
                return "Error: BackupId is required for diff action";
            }

            var diff = backupManager.GetBackupDifference(arguments.BackupId, arguments.FilePath);
            return diff;
        }

        private string HandleCleanupAction(LogiqBackupManager backupManager, BackupCommandArguments arguments)
        {
            var beforeCount = backupManager.GetManifest().Backups.Count;
            backupManager.CleanupOldBackups(arguments.RetentionDays);
            var afterCount = backupManager.GetManifest().Backups.Count;
            var removed = beforeCount - afterCount;

            return $"Cleanup completed. Removed {removed} old backup(s). {afterCount} backup(s) remaining.";
        }

        private string HandleStatusAction(LogiqBackupManager backupManager)
        {
            var manifest = backupManager.GetManifest();
            var result = new StringBuilder();
            
            result.AppendLine("Backup System Status:");
            result.AppendLine($"  Workspace: {manifest.WorkspacePath}");
            result.AppendLine($"  Created: {manifest.Created:yyyy-MM-dd HH:mm:ss} UTC");
            result.AppendLine($"  Last Modified: {manifest.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
            result.AppendLine($"  Total Backups: {manifest.Backups.Count}");
            result.AppendLine();
            
            result.AppendLine("Settings:");
            result.AppendLine($"  Max Backups per File: {manifest.Settings.MaxBackupsPerFile}");
            result.AppendLine($"  Auto Cleanup: {manifest.Settings.AutoCleanup}");
            result.AppendLine($"  Retention Days: {manifest.Settings.RetentionDays}");
            result.AppendLine($"  Compression: {manifest.Settings.CompressionEnabled}");
            result.AppendLine();

            if (manifest.Backups.Any())
            {
                var fileGroups = manifest.Backups.GroupBy(b => b.OriginalPath).ToList();
                result.AppendLine($"Files with Backups ({fileGroups.Count}):");
                
                foreach (var group in fileGroups.Take(10))
                {
                    var latest = group.OrderByDescending(b => b.Timestamp).First();
                    var count = group.Count();
                    var timeAgo = GetTimeAgo(latest.Timestamp);
                    
                    result.AppendLine($"  {group.Key} ({count} backup{(count > 1 ? "s" : "")}, latest: {timeAgo})");
                }
                
                if (fileGroups.Count > 10)
                {
                    result.AppendLine($"  ... and {fileGroups.Count - 10} more files");
                }
            }

            return result.ToString();
        }

        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;
            
            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{(timeSpan.TotalDays >= 60 ? "s" : "")} ago";
            
            return $"{(int)(timeSpan.TotalDays / 365)} year{(timeSpan.TotalDays >= 730 ? "s" : "")} ago";
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

    public class BackupCommandArguments
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;
        
        [JsonPropertyName("backupId")]
        public string? BackupId { get; set; }
        
        [JsonPropertyName("filePath")]
        public string? FilePath { get; set; }
        
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
        
        [JsonPropertyName("retentionDays")]
        public int? RetentionDays { get; set; }
    }
} 
