using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LogiQCLI.Tools.GitHub.Objects
{
    public class CompareBranchesArguments
    {
        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("repo")]
        public string? Repo { get; set; }

        [JsonPropertyName("baseRef")]
        public string? BaseRef { get; set; }

        [JsonPropertyName("headRef")]
        public string? HeadRef { get; set; }
    }
}
