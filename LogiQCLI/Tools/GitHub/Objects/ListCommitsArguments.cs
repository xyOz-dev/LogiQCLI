using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class ListCommitsArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("sha")]
        public string? Sha { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("since")]
        public string? Since { get; set; }

        [JsonPropertyName("until")]
        public string? Until { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}
