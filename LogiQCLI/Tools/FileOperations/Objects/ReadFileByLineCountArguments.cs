using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class ReadFileByLineCountArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
        
        [JsonPropertyName("lineCount")]
        public int LineCount { get; set; }
    }
}
