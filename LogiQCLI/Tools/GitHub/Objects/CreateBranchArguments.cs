using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class CreateBranchArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("branchName")]
        public string? BranchName { get; set; }

        [JsonPropertyName("fromBranch")]
        public string? FromBranch { get; set; }

        [JsonPropertyName("fromSha")]
        public string? FromSha { get; set; }
    }
}