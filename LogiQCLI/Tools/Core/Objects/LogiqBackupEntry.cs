using System;
using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Core.Objects
{
    public class LogiqBackupEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("originalPath")]
        public string OriginalPath { get; set; } = string.Empty;
        
        [JsonPropertyName("backupPath")]
        public string BackupPath { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;
        
        [JsonPropertyName("tool")]
        public string Tool { get; set; } = string.Empty;
        
        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
        
        [JsonPropertyName("checksum")]
        public string Checksum { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
} 