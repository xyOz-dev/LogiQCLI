using System;
using System.Collections.Generic;
using System.Linq;
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
    [ToolMetadata("GitHub", Tags = new[] { "github", "safe", "query" })]
    public class GetIssueTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetIssueTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_issue",
                Description = "Get detailed information about specific GitHub issues including title, body, labels, assignees, and metadata.",
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
                            description = "Issue number to retrieve. Must be existing issue."
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
                var arguments = JsonSerializer.Deserialize<GetIssueArguments>(args);
                if (arguments == null || arguments.IssueNumber <= 0)
                {
                    return "Error: Invalid arguments. Issue number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var issue = await _gitHubClient.GetIssueAsync(arguments.Owner, arguments.Repo, arguments.IssueNumber);

                var result = $"Issue #{issue.Number}: {issue.Title}\n\n";
                result += $"Details:\n";
                result += $"  State: {issue.State}\n";
                result += $"  Author: {issue.User.Login}\n";
                result += $"  Created: {issue.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                result += $"  Updated: {issue.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";

                if (issue.ClosedAt.HasValue)
                {
                    result += $"  Closed: {issue.ClosedAt.Value:yyyy-MM-dd HH:mm:ss} UTC\n";
                }

                if (issue.Labels.Any())
                {
                    result += $"  Labels: {string.Join(", ", issue.Labels.Select(l => l.Name))}\n";
                }

                if (issue.Assignees.Any())
                {
                    result += $"  Assignees: {string.Join(", ", issue.Assignees.Select(a => a.Login))}\n";
                }

                if (issue.Milestone != null)
                {
                    result += $"  Milestone: {issue.Milestone.Title}\n";
                }

                result += $"  Comments: {issue.Comments}\n";
                result += $"  URL: {issue.HtmlUrl}\n\n";

                result += $"Body:\n{issue.Body ?? "No description provided"}";

                return result;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub issue: {ex.Message}";
            }
        }
    }
}
