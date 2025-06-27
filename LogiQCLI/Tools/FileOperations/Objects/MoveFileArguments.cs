using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class MoveFileArguments
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
        
        [JsonPropertyName("destination")]
        public string Destination { get; set; } = string.Empty;
        
        [JsonPropertyName("overwrite")]
        public bool? Overwrite { get; set; }
        
        [JsonPropertyName("createDirectory")]
        public bool? CreateDirectory { get; set; }
    }
}
