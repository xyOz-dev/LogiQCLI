using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.GitHub.Objects
{
    public class GetPullRequestDiffArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("pullRequestNumber")]
        public int PullRequestNumber { get; set; }

        [JsonPropertyName("maxLines")]
        public int? MaxLines { get; set; }
    }
}
