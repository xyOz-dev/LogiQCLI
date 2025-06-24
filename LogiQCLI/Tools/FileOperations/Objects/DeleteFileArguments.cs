using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class DeleteFileArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }
        
        [JsonPropertyName("recursive")]
        public bool? Recursive { get; set; }
        
        [JsonPropertyName("force")]
        public bool? Force { get; set; }
    }
}