using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.FileOperations.Arguments
{
    internal class ReadFileArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }
    }
}