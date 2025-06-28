using System.Text.Json.Serialization;

namespace LogiQCLI.Tools.GitHub.Objects
{
    internal class MarkNotificationAsReadArguments
    {
        [JsonPropertyName("notificationId")]
        public string? NotificationId { get; set; }
    }
} 