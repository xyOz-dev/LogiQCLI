using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Octokit;

using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.GitHub.Objects;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "update" })]
    public class UpdateIssueTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public UpdateIssueTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "update_github_issue",
                Description = "Update GitHub issue title, body, labels, assignees, or state. Labels and assignees are replaced entirely. Requires GitHub authentication token.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        owner = new
                        {
                            type = "string",
                            description = "Repository owner (username or organization). Required unless default configured."
                        },
                        repo = new
                        {
                            type = "string",
                            description = "Repository name. Required unless default configured."
                        },
                        issueNumber = new
                        {
                            type = "integer",
                            description = "Issue number to update. Must be existing issue."
                        },
                        title = new
                        {
                            type = "string",
                            description = "New issue title. Leave empty to keep current title."
                        },
                        body = new
                        {
                            type = "string",
                            description = "New issue body. Supports GitHub markdown. Leave empty to keep current body."
                        },
                        state = new
                        {
                            type = "string",
                            description = "New issue state. Options: 'open', 'closed'. Leave empty to keep current state."
                        },
                        labels = new
                        {
                            type = "array",
                            description = "Array of label names to REPLACE current labels entirely. Example: ['bug', 'priority-high']",
                            items = new { type = "string" }
                        },
                        assignees = new
                        {
                            type = "array",
                            description = "Array of GitHub usernames to REPLACE current assignees entirely. Example: ['john-doe', 'jane-smith']",
                            items = new { type = "string" }
                        }
                    },
                    Required = new[] { "issueNumber" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<UpdateIssueArguments>(args);
                if (arguments == null || arguments.IssueNumber <= 0)
                {
                    return "Error: Invalid arguments. Issue number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var issueUpdate = new IssueUpdate();
                bool hasUpdates = false;

                if (!string.IsNullOrEmpty(arguments.Title))
                {
                    issueUpdate.Title = arguments.Title;
                    hasUpdates = true;
                }

                if (!string.IsNullOrEmpty(arguments.Body))
                {
                    issueUpdate.Body = arguments.Body;
                    hasUpdates = true;
                }

                if (!string.IsNullOrEmpty(arguments.State))
                {
                    if (Enum.TryParse<ItemState>(arguments.State, true, out var itemState))
                    {
                        issueUpdate.State = itemState;
                        hasUpdates = true;
                    }
                    else
                    {
                        return $"Error: Invalid state '{arguments.State}'. Must be 'open' or 'closed'.";
                    }
                }

                if (arguments.Labels != null)
                {
                    issueUpdate.Labels.Clear();
                    foreach (var label in arguments.Labels)
                    {
                        issueUpdate.Labels.Add(label);
                    }
                    hasUpdates = true;
                }

                if (arguments.Assignees != null)
                {
                    issueUpdate.Assignees.Clear();
                    foreach (var assignee in arguments.Assignees)
                    {
                        issueUpdate.Assignees.Add(assignee);
                    }
                    hasUpdates = true;
                }

                if (!hasUpdates)
                {
                    return "Error: No updates specified. Provide at least one field to update (title, body, state, labels, or assignees).";
                }

                var issue = await _gitHubClient.UpdateIssueAsync(arguments.Owner, arguments.Repo, arguments.IssueNumber, issueUpdate);

                var result = $"Successfully updated issue #{issue.Number}: {issue.Title}\n";
                result += $"State: {issue.State}\n";
                result += $"Updated: {issue.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                result += $"URL: {issue.HtmlUrl}\n";

                if (issue.Labels.Count > 0)
                {
                    result += $"Labels: {string.Join(", ", issue.Labels.Select(l => l.Name))}\n";
                }

                if (issue.Assignees.Count > 0)
                {
                    result += $"Assignees: {string.Join(", ", issue.Assignees.Select(a => a.Login))}\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error updating GitHub issue: {ex.Message}";
            }
        }
    }
}
