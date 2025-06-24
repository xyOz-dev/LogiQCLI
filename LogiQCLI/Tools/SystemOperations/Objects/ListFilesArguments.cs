using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.SystemOperations.Objects
{
    internal class ListFilesArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }
        
        [JsonPropertyName("recursive")]
        public bool? Recursive { get; set; }
    }
}