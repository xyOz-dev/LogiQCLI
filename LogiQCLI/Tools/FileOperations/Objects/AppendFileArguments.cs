using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class AppendFileArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
        [JsonPropertyName("newline")]
        public bool? Newline { get; set; }
    }
}
