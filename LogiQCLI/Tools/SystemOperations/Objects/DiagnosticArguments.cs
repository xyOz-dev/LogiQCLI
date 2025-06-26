using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.SystemOperations.Objects
{
    internal class DiagnosticArguments
    {
        [JsonPropertyName("show_last_n")]
        public int ShowLastN { get; set; } = 5;

        [JsonPropertyName("include_content")]
        public bool IncludeContent { get; set; } = true;
    }
}
