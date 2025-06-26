using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.ContentManipulation.Objects
{
    internal class SearchAndReplaceArguments
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }
        
        [JsonPropertyName("search")]
        public string Search { get; set; }
        
        [JsonPropertyName("replace")]
        public string Replace { get; set; }
        
        [JsonPropertyName("useRegex")]
        public bool? UseRegex { get; set; }
        
        [JsonPropertyName("caseSensitive")]
        public bool? CaseSensitive { get; set; }
        
        [JsonPropertyName("multiline")]
        public bool? Multiline { get; set; }
        
        [JsonPropertyName("backup")]
        public bool? Backup { get; set; }
    }
}
