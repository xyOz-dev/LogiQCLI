using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class CommentOnPullRequestArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("pullRequestNumber")]
        public int PullRequestNumber { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }
} 