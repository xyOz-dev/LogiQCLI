using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.Documentation.Objects
{
    public class ResolveLibraryIdArguments
    {
        [JsonPropertyName("libraryName")]
        public string LibraryName { get; set; } = string.Empty;
    }
} 