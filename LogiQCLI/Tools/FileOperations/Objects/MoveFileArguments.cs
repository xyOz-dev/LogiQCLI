using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class MoveFileArguments
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }
        
        [JsonPropertyName("destination")]
        public string Destination { get; set; }
        
        [JsonPropertyName("overwrite")]
        public bool? Overwrite { get; set; }
        
        [JsonPropertyName("createDirectory")]
        public bool? CreateDirectory { get; set; }
    }
}
