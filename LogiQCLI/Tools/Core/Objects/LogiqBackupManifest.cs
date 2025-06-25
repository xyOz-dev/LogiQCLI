using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Core.Objects
{
    public class LogiqBackupManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";
        
        [JsonPropertyName("workspacePath")]
        public string WorkspacePath { get; set; } = string.Empty;
        
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
        
        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }
        
        [JsonPropertyName("backups")]
        public List<LogiqBackupEntry> Backups { get; set; } = new List<LogiqBackupEntry>();
        
        [JsonPropertyName("settings")]
        public LogiqBackupSettings Settings { get; set; } = new LogiqBackupSettings();
    }
    
    public class LogiqBackupSettings
    {
        [JsonPropertyName("maxBackupsPerFile")]
        public int MaxBackupsPerFile { get; set; } = 10;
        
        [JsonPropertyName("autoCleanup")]
        public bool AutoCleanup { get; set; } = true;
        
        [JsonPropertyName("retentionDays")]
        public int RetentionDays { get; set; } = 30;
        
        [JsonPropertyName("compressionEnabled")]
        public bool CompressionEnabled { get; set; } = false;
    }
} 