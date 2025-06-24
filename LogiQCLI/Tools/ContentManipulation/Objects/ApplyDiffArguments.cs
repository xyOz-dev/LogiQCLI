using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.ContentManipulation.Objects
{
    public class ApplyDiffArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("original")]
        public string Original { get; set; } = string.Empty;

        [JsonPropertyName("replacement")]
        public string Replacement { get; set; } = string.Empty;

        [JsonPropertyName("maxReplacements")]
        public int MaxReplacements { get; set; } = 1;

        [JsonPropertyName("useRegex")]
        public bool UseRegex { get; set; } = false;

        [JsonPropertyName("createBackup")]
        public bool CreateBackup { get; set; } = true;

        [JsonPropertyName("preview")]
        public bool Preview { get; set; } = false;
    }
}