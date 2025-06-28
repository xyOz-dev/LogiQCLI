using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Tools.Core
{
    public class LogiqBackupManager
    {
        private readonly string _workspacePath;
        private readonly string _logiqFilePath;
        private readonly string _backupDirectoryPath;
        private LogiqBackupManifest? _manifest;
        private readonly object _lockObject = new object();

        public LogiqBackupManager(string? workspacePath = null)
        {
            _workspacePath = workspacePath ?? Directory.GetCurrentDirectory();
            _logiqFilePath = Path.Combine(_workspacePath, ".logiq");
            _backupDirectoryPath = Path.Combine(_workspacePath, ".logiq-backups");
            
            EnsureBackupDirectoryExists();
            LoadOrCreateManifest();
        }

        public Task<string> CreateBackupAsync(string filePath, string content, string tool, string operation, string description = "")
        {
            lock (_lockObject)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(_workspacePath, Path.GetFullPath(filePath));
                    var backupId = GenerateBackupId();
                    var timestamp = DateTime.UtcNow;
                    

                    var backupRelativePath = CreateBackupFilePath(relativePath, backupId, timestamp);
                    var backupFullPath = Path.Combine(_backupDirectoryPath, backupRelativePath);
                    

                    var backupDir = Path.GetDirectoryName(backupFullPath);
                    if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }
                    

                    File.WriteAllText(backupFullPath, content);
                    

                    var checksum = CalculateMD5(content);
                    

                    var backupEntry = new LogiqBackupEntry
                    {
                        Id = backupId,
                        OriginalPath = relativePath,
                        BackupPath = backupRelativePath,
                        Timestamp = timestamp,
                        Operation = operation,
                        Tool = tool,
                        FileSize = content.Length,
                        Checksum = checksum,
                        Description = description
                    };
                    

                    _manifest!.Backups.Add(backupEntry);
                    _manifest.LastModified = timestamp;
                    

                    CleanupOldBackupsInternal(relativePath);
                    

                    SaveManifest();
                    
                    return Task.FromResult(backupId);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create backup: {ex.Message}", ex);
                }
            }
        }

        public List<LogiqBackupEntry> ListBackups(string? filePath = null, int? limit = null)
        {
            lock (_lockObject)
            {
                var query = _manifest!.Backups.AsEnumerable();
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    var relativePath = Path.GetRelativePath(_workspacePath, Path.GetFullPath(filePath));
                    query = query.Where(b => b.OriginalPath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
                }
                
                query = query.OrderByDescending(b => b.Timestamp);
                
                if (limit.HasValue)
                {
                    query = query.Take(limit.Value);
                }
                
                return query.ToList();
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupId, string? targetPath = null)
        {
            try
            {
                LogiqBackupEntry? backup;
                string backupFullPath;
                string content;
                

                lock (_lockObject)
                {
                    backup = _manifest!.Backups.FirstOrDefault(b => b.Id == backupId);
                    if (backup == null)
                    {
                        return false;
                    }
                    
                    backupFullPath = Path.Combine(_backupDirectoryPath, backup.BackupPath);
                }
                
                if (!File.Exists(backupFullPath))
                {
                    return false;
                }
                
                content = File.ReadAllText(backupFullPath);
                

                var currentChecksum = CalculateMD5(content);
                if (currentChecksum != backup.Checksum)
                {
                    throw new InvalidOperationException($"Backup integrity check failed for {backupId}");
                }
                

                var restoreTarget = targetPath ?? Path.Combine(_workspacePath, backup.OriginalPath);
                var restoreDir = Path.GetDirectoryName(restoreTarget);
                
                if (!string.IsNullOrEmpty(restoreDir) && !Directory.Exists(restoreDir))
                {
                    Directory.CreateDirectory(restoreDir);
                }
                

                if (File.Exists(restoreTarget))
                {
                    var currentContent = File.ReadAllText(restoreTarget);
                    await CreateBackupAsync(restoreTarget, currentContent, "LogiqBackupManager", "pre-restore", 
                        $"Automatic backup before restoring {backupId}");
                }
                

                File.WriteAllText(restoreTarget, content);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetBackupDifference(string backupId, string? comparisonPath = null)
        {
            lock (_lockObject)
            {
                var backup = _manifest!.Backups.FirstOrDefault(b => b.Id == backupId);
                if (backup == null)
                {
                    return "Backup not found";
                }
                
                var backupFullPath = Path.Combine(_backupDirectoryPath, backup.BackupPath);
                if (!File.Exists(backupFullPath))
                {
                    return "Backup file not found";
                }
                
                var backupContent = File.ReadAllText(backupFullPath);
                

                var compareTarget = comparisonPath ?? Path.Combine(_workspacePath, backup.OriginalPath);
                if (!File.Exists(compareTarget))
                {
                    return $"Original file no longer exists: {backup.OriginalPath}";
                }
                
                var currentContent = File.ReadAllText(compareTarget);
                

                var backupLines = backupContent.Split('\n');
                var currentLines = currentContent.Split('\n');
                
                var differences = new StringBuilder();
                differences.AppendLine($"Comparing backup {backupId} with current file:");
                differences.AppendLine($"Backup: {backupLines.Length} lines");
                differences.AppendLine($"Current: {currentLines.Length} lines");
                differences.AppendLine();
                
                var maxLines = Math.Max(backupLines.Length, currentLines.Length);
                var diffCount = 0;
                
                for (int i = 0; i < maxLines && diffCount < 20; i++)
                {
                    var backupLine = i < backupLines.Length ? backupLines[i] : "<end of file>";
                    var currentLine = i < currentLines.Length ? currentLines[i] : "<end of file>";
                    
                    if (backupLine != currentLine)
                    {
                        differences.AppendLine($"Line {i + 1}:");
                        differences.AppendLine($"  Backup:  {backupLine}");
                        differences.AppendLine($"  Current: {currentLine}");
                        differences.AppendLine();
                        diffCount++;
                    }
                }
                
                if (diffCount == 0)
                {
                    differences.AppendLine("No differences found.");
                }
                else if (diffCount >= 20)
                {
                    differences.AppendLine("... (showing first 20 differences)");
                }
                
                return differences.ToString();
            }
        }

        public void CleanupOldBackups(int? retentionDays = null, int? maxBackupsPerFile = null)
        {
            lock (_lockObject)
            {
                CleanupOldBackupsInternal(null, retentionDays, maxBackupsPerFile);
            }
        }

        private void CleanupOldBackupsInternal(string? specificFilePath = null, int? retentionDays = null, int? maxBackupsPerFile = null)
        {
            if (_manifest == null) return;
            
            var retention = retentionDays ?? _manifest.Settings.RetentionDays;
            var maxPerFile = maxBackupsPerFile ?? _manifest.Settings.MaxBackupsPerFile;
            var cutoffDate = DateTime.UtcNow.AddDays(-retention);
            
            var backupsByFile = _manifest.Backups.GroupBy(b => b.OriginalPath).ToList();
            
            if (!string.IsNullOrEmpty(specificFilePath))
            {
                backupsByFile = backupsByFile.Where(g => g.Key.Equals(specificFilePath, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            var toRemove = new List<LogiqBackupEntry>();
            
            foreach (var fileGroup in backupsByFile)
            {
                var backups = fileGroup.OrderByDescending(b => b.Timestamp).ToList();
                
                var oldBackups = backups.Where(b => b.Timestamp < cutoffDate).ToList();
                toRemove.AddRange(oldBackups);
                
                var excessBackups = backups.Skip(maxPerFile).ToList();
                toRemove.AddRange(excessBackups);
            }
            
            foreach (var backup in toRemove.Distinct())
            {
                var backupFullPath = Path.Combine(_backupDirectoryPath, backup.BackupPath);
                if (File.Exists(backupFullPath))
                {
                    File.Delete(backupFullPath);
                }
                _manifest.Backups.Remove(backup);
            }
            
            if (toRemove.Any())
            {
                _manifest.LastModified = DateTime.UtcNow;
                SaveManifest();
            }
        }

        private void EnsureBackupDirectoryExists()
        {
            if (!Directory.Exists(_backupDirectoryPath))
            {
                Directory.CreateDirectory(_backupDirectoryPath);
            }
        }

        private void LoadOrCreateManifest()
        {
            if (File.Exists(_logiqFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_logiqFilePath);
                    _manifest = JsonSerializer.Deserialize<LogiqBackupManifest>(json) ?? CreateNewManifest();
                }
                catch
                {
                    _manifest = CreateNewManifest();
                }
            }
            else
            {
                _manifest = CreateNewManifest();
                SaveManifest();
            }
        }

        private LogiqBackupManifest CreateNewManifest()
        {
            return new LogiqBackupManifest
            {
                WorkspacePath = _workspacePath,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
        }

        private void SaveManifest()
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(_manifest, options);
            File.WriteAllText(_logiqFilePath, json);
        }

        private string GenerateBackupId()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-") + Guid.NewGuid().ToString("N")[..8];
        }

        private string CreateBackupFilePath(string originalPath, string backupId, DateTime timestamp)
        {
            var directory = Path.GetDirectoryName(originalPath) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            
            var backupFileName = $"{fileName}.{backupId}{extension}";
            return Path.Combine(directory, backupFileName).Replace('\\', '/');
        }

        private string CalculateMD5(string content)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        public LogiqBackupManifest GetManifest()
        {
            lock (_lockObject)
            {
                return _manifest!;
            }
        }
    }
} 
