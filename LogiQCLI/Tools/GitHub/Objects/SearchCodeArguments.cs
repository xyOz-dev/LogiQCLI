using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class SearchCodeArguments
    {
        [JsonPropertyName("query")]
        public string? Query { get; set; }

        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}