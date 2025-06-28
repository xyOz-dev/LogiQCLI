using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Core.Objects
{
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