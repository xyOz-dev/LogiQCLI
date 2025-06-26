using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.GitHub.Objects
{
    public class ListIssueCommentsArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("issueNumber")]
        public int IssueNumber { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}
