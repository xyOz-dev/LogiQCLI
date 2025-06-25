using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "update" })]
    public class MarkNotificationAsReadTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public MarkNotificationAsReadTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "mark_github_notification_as_read",
                Description = "Mark specific GitHub notification as read using notification ID. Get ID from list_github_notifications tool. Requires GitHub authentication token.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        notificationId = new
                        {
                            type = "string",
                            description = "Notification ID to mark as read. Get from list_github_notifications tool."
                        }
                    },
                    Required = new[] { "notificationId" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<MarkNotificationAsReadArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.NotificationId))
                {
                    return "Error: Invalid arguments. Notification ID is required.";
                }

                await _gitHubClient.MarkNotificationAsReadAsync(arguments.NotificationId);

                return $"Successfully marked notification {arguments.NotificationId} as read.";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error marking GitHub notification as read: {ex.Message}";
            }
        }

        internal class MarkNotificationAsReadArguments
        {
            [JsonPropertyName("notificationId")]
            public string? NotificationId { get; set; }
        }
    }
}