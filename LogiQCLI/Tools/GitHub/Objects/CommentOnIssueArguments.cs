using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class CommentOnIssueArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("issueNumber")]
        public int IssueNumber { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }
}
