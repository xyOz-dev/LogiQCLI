using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class UpdateFileArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("sha")]
        public string? Sha { get; set; }

        [JsonPropertyName("branch")]
        public string? Branch { get; set; }

        [JsonPropertyName("authorName")]
        public string? AuthorName { get; set; }

        [JsonPropertyName("authorEmail")]
        public string? AuthorEmail { get; set; }
    }
}
