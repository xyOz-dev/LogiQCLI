using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Documentation.Objects
{
    public class GetLibraryDocsArguments
    {
        [JsonPropertyName("libraryId")]
        public string LibraryId { get; set; } = string.Empty;

        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("tokens")]
        public int? Tokens { get; set; }
    }
} 