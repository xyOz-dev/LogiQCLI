using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class DeleteFileArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
        
        [JsonPropertyName("recursive")]
        public bool? Recursive { get; set; }
        
        [JsonPropertyName("force")]
        public bool? Force { get; set; }
    }
}
