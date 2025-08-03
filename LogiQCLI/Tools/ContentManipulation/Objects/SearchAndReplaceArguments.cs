using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.ContentManipulation.Objects
{
    internal class SearchAndReplaceArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
        
        [JsonPropertyName("search")]
        public string Search { get; set; } = string.Empty;
        
        [JsonPropertyName("replace")]
        public string Replace { get; set; } = string.Empty;
        
        [JsonPropertyName("useRegex")]
        public bool? UseRegex { get; set; }
        
        [JsonPropertyName("caseSensitive")]
        public bool? CaseSensitive { get; set; }
        
        [JsonPropertyName("multiline")]
        public bool? Multiline { get; set; }
        
        [JsonPropertyName("backup")]
        public bool? Backup { get; set; }
        
        [JsonPropertyName("dryRun")]
        public bool? DryRun { get; set; }
        
        [JsonPropertyName("dotAll")]
        public bool? DotAll { get; set; }
        
        [JsonPropertyName("showProgress")]
        public bool? ShowProgress { get; set; }
    }
}
