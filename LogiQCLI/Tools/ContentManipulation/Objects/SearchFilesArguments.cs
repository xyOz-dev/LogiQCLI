using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.ContentManipulation.Arguments
{
    internal class SearchFilesArguments
    {
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("file_pattern")]
        public string FilePattern { get; set; }

        [JsonPropertyName("use_regex")]
        public bool? UseRegex { get; set; }

        [JsonPropertyName("case_sensitive")]
        public bool? CaseSensitive { get; set; }

        [JsonPropertyName("max_results")]
        public int? MaxResults { get; set; }
    }
}
