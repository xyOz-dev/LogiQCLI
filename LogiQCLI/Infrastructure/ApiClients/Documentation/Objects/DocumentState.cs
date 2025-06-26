using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.Documentation.Objects
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentState
    {
        Initial,
        Finalized,
        Error,
        Delete
    }
} 