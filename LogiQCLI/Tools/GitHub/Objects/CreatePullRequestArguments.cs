using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class CreatePullRequestArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("head")]
        public string? Head { get; set; }

        [JsonPropertyName("base")]
        public string? BaseRef { get; set; }

        [JsonPropertyName("draft")]
        public bool? Draft { get; set; }
    }
}