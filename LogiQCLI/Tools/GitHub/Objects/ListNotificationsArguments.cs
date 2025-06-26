using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class ListNotificationsArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("all")]
        public bool? All { get; set; }

        [JsonPropertyName("participating")]
        public bool? Participating { get; set; }

        [JsonPropertyName("since")]
        public string? Since { get; set; }

        [JsonPropertyName("before")]
        public string? Before { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}
