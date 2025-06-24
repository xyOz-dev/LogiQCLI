using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class CreateFileArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("overwrite")]
        public bool? Overwrite { get; set; }
    }
}