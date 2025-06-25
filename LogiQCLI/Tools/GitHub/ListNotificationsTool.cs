using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Octokit;

using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.GitHub.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "safe", "query" })]
    public class ListNotificationsTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListNotificationsTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_notifications",
                Description = "List GitHub notifications with filtering by repository, participation, and read status. Requires GitHub authentication token.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        owner = new
                        {
                            type = "string",
                            description = "Repository owner to filter notifications. Leave empty for all repositories."
                        },
                        repo = new
                        {
                            type = "string",
                            description = "Repository name to filter notifications. Requires owner to be specified."
                        },
                        all = new
                        {
                            type = "boolean",
                            description = "Include read notifications. Default: false (only unread)"
                        },
                        participating = new
                        {
                            type = "boolean",
                            description = "Only notifications where user is directly participating. Default: false"
                        },
                        since = new
                        {
                            type = "string",
                            description = "ISO 8601 date string for notifications since. Example: '2023-01-01T00:00:00Z'"
                        },
                        before = new
                        {
                            type = "string",
                            description = "ISO 8601 date string for notifications before. Example: '2023-12-31T23:59:59Z'"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum notifications to return. Default: 30, Maximum: 100"
                        }
                    },
                    Required = new string[] { }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ListNotificationsArguments>(args) ?? new ListNotificationsArguments();

                var request = new NotificationsRequest
                {
                    All = arguments.All ?? false,
                    Participating = arguments.Participating ?? false
                };

                if (!string.IsNullOrEmpty(arguments.Since) && DateTime.TryParse(arguments.Since, out var sinceDate))
                {
                    request.Since = sinceDate;
                }

                if (!string.IsNullOrEmpty(arguments.Before) && DateTime.TryParse(arguments.Before, out var beforeDate))
                {
                    request.Before = beforeDate;
                }

                var notifications = !string.IsNullOrEmpty(arguments.Owner) && !string.IsNullOrEmpty(arguments.Repo)
                    ? await _gitHubClient.GetRepositoryNotificationsAsync(arguments.Owner, arguments.Repo, request)
                    : await _gitHubClient.GetNotificationsAsync(request);

                var limit = Math.Min(arguments.Limit ?? 30, 100);
                var limitedNotifications = notifications.Take(limit).ToList();

                if (!limitedNotifications.Any())
                {
                    var scope = !string.IsNullOrEmpty(arguments.Owner) && !string.IsNullOrEmpty(arguments.Repo)
                        ? $" for {arguments.Owner}/{arguments.Repo}"
                        : "";
                    var status = arguments.All == true ? "" : " unread";
                    return $"No{status} notifications found{scope}.";
                }

                var unreadCount = limitedNotifications.Count(n => n.Unread);
                var repositoryScope = !string.IsNullOrEmpty(arguments.Owner) && !string.IsNullOrEmpty(arguments.Repo)
                    ? $" for {arguments.Owner}/{arguments.Repo}"
                    : "";

                var result = $"Found {limitedNotifications.Count} notifications{repositoryScope} ({unreadCount} unread):\n\n";

                foreach (var notification in limitedNotifications)
                {
                    var status = notification.Unread ? "🔔 UNREAD" : "✓ Read";
                    result += $"{status} - {notification.Subject.Title}\n";
                    result += $"  Repository: {notification.Repository.FullName}\n";
                    result += $"  Type: {notification.Subject.Type}\n";
                    result += $"  Reason: {notification.Reason}\n";
                    result += $"  Updated: {notification.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                    
                    if (!string.IsNullOrEmpty(notification.Subject.Url))
                    {
                        result += $"  Subject URL: {notification.Subject.Url}\n";
                    }

                    result += $"  Notification ID: {notification.Id}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub notifications: {ex.Message}";
            }
        }
    }
}
