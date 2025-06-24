using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;
using LogiQCLI.Tools.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "update" })]
    public class MarkAllNotificationsAsReadTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public MarkAllNotificationsAsReadTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "mark_all_github_notifications_as_read",
                Description = "Marks all GitHub notifications as read for the authenticated user. " +
                              "Requires GitHub authentication token. Use this tool for bulk notification management.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        confirm = new
                        {
                            type = "boolean",
                            description = "Confirmation flag to prevent accidental bulk operations. " +
                                         "Must be set to true to execute the operation."
                        }
                    },
                    Required = new[] { "confirm" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<MarkAllNotificationsAsReadArguments>(args);
                if (arguments == null || arguments.Confirm != true)
                {
                    return "Error: Confirmation required. Set 'confirm' to true to mark all notifications as read.";
                }

                await _gitHubClient.MarkAllNotificationsAsReadAsync();

                return "Successfully marked all notifications as read.";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error marking all GitHub notifications as read: {ex.Message}";
            }
        }
    }
}