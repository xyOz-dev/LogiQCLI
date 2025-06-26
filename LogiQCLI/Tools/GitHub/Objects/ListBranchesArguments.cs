using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class ListBranchesArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}
