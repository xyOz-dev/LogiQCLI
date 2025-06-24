using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class ListIssuesArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("labels")]
        public string[]? Labels { get; set; }

        [JsonPropertyName("assignee")]
        public string? Assignee { get; set; }

        [JsonPropertyName("creator")]
        public string? Creator { get; set; }

        [JsonPropertyName("mentioned")]
        public string? Mentioned { get; set; }

        [JsonPropertyName("milestone")]
        public string? Milestone { get; set; }

        [JsonPropertyName("sort")]
        public string? Sort { get; set; }

        [JsonPropertyName("direction")]
        public string? Direction { get; set; }

        [JsonPropertyName("since")]
        public string? Since { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}