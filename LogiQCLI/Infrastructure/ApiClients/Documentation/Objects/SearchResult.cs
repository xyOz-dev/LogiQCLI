using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.Documentation.Objects
{
    public class SearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("branch")]
        public string Branch { get; set; } = string.Empty;

        [JsonPropertyName("lastUpdateDate")]
        public string LastUpdateDate { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public DocumentState State { get; set; }

        [JsonPropertyName("totalTokens")]
        public object? TotalTokensRaw { get; set; }

        [JsonPropertyName("totalSnippets")]
        public object? TotalSnippetsRaw { get; set; }

        [JsonPropertyName("totalPages")]
        public object? TotalPagesRaw { get; set; }

        [JsonIgnore]
        public int TotalTokens 
        { 
            get 
            {
                if (TotalTokensRaw == null) return 0;
                if (TotalTokensRaw is int intValue) return intValue;
                if (TotalTokensRaw is string strValue && int.TryParse(strValue, out int parsedValue)) return parsedValue;
                return 0;
            } 
        }

        [JsonIgnore]
        public int TotalSnippets 
        { 
            get 
            {
                if (TotalSnippetsRaw == null) return -1;
                if (TotalSnippetsRaw is int intValue) return intValue;
                if (TotalSnippetsRaw is string strValue && int.TryParse(strValue, out int parsedValue)) return parsedValue;
                return -1;
            } 
        }

        [JsonIgnore]
        public int TotalPages 
        { 
            get 
            {
                if (TotalPagesRaw == null) return 0;
                if (TotalPagesRaw is int intValue) return intValue;
                if (TotalPagesRaw is string strValue && int.TryParse(strValue, out int parsedValue)) return parsedValue;
                return 0;
            } 
        }

        [JsonPropertyName("stars")]
        public object? StarsRaw { get; set; }

        [JsonIgnore]
        public int? Stars 
        { 
            get 
            {
                if (StarsRaw == null) return null;
                if (StarsRaw is int intValue) return intValue;
                if (StarsRaw is string strValue && int.TryParse(strValue, out int parsedValue)) return parsedValue;
                return null;
            } 
        }

        [JsonPropertyName("trustScore")]
        public object? TrustScoreRaw { get; set; }

        [JsonIgnore]
        public int? TrustScore 
        { 
            get 
            {
                if (TrustScoreRaw == null) return null;
                if (TrustScoreRaw is int intValue) return intValue;
                if (TrustScoreRaw is string strValue && int.TryParse(strValue, out int parsedValue)) return parsedValue;
                return null;
            } 
        }

        [JsonPropertyName("versions")]
        public string[]? Versions { get; set; }
    }
} 