using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class MergePullRequestArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("pullRequestNumber")]
        public int PullRequestNumber { get; set; }

        [JsonPropertyName("commitTitle")]
        public string? CommitTitle { get; set; }

        [JsonPropertyName("commitMessage")]
        public string? CommitMessage { get; set; }

        [JsonPropertyName("mergeMethod")]
        public string? MergeMethod { get; set; }

        [JsonPropertyName("sha")]
        public string? Sha { get; set; }
    }
}
