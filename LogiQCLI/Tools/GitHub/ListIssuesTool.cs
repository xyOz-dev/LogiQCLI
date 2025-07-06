using System;
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
    public class ListIssuesTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListIssuesTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_issues",
                Description = "List repository issues with filtering by state, labels, assignee, and dates.",
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
                        state = new
                        {
                            type = "string",
                            description = "Issue state filter. Options: 'open', 'closed', 'all'. Default: 'open'"
                        },
                        labels = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Array of label names. Only issues with ALL specified labels returned. Example: ['bug', 'priority-high']"
                        },
                        assignee = new
                        {
                            type = "string",
                            description = "GitHub username filter. Use 'none' for unassigned, '*' for any assigned."
                        },
                        since = new
                        {
                            type = "string",
                            description = "ISO 8601 date string for issues updated since. Example: '2023-01-01T00:00:00Z'"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum issues to return. Default: 30, Maximum: 100"
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
                var arguments = JsonSerializer.Deserialize<ListIssuesArguments>(args) ?? new ListIssuesArguments();

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var request = new RepositoryIssueRequest();

                if (!string.IsNullOrEmpty(arguments.State))
                {
                    if (Enum.TryParse<ItemStateFilter>(arguments.State, true, out var stateFilter))
                    {
                        request.State = stateFilter;
                    }
                }

                if (arguments.Labels != null && arguments.Labels.Length > 0)
                {
                    var labels = arguments.Labels.Where(l => !string.IsNullOrWhiteSpace(l))
                                                .Select(l => l.Trim());
                    foreach (var label in labels)
                    {
                        request.Labels.Add(label);
                    }
                }

                if (!string.IsNullOrEmpty(arguments.Assignee))
                {
                    request.Assignee = arguments.Assignee;
                }

                if (!string.IsNullOrEmpty(arguments.Since) && DateTime.TryParse(arguments.Since, out var sinceDate))
                {
                    request.Since = sinceDate;
                }

                var issues = await _gitHubClient.GetIssuesAsync(arguments.Owner, arguments.Repo, request);

                var limit = Math.Min(arguments.Limit ?? 30, 100);
                var limitedIssues = issues.Take(limit).ToList();

                if (!limitedIssues.Any())
                {
                    return $"No issues found in {arguments.Owner}/{arguments.Repo} with the specified criteria.";
                }

                var result = $"Found {limitedIssues.Count} issues in {arguments.Owner}/{arguments.Repo}:\n\n";

                foreach (var issue in limitedIssues)
                {
                    var labels = issue.Labels.Any() ? string.Join(", ", issue.Labels.Select(l => l.Name)) : "None";
                    var assignees = issue.Assignees.Any() ? string.Join(", ", issue.Assignees.Select(a => a.Login)) : "Unassigned";

                    result += $"#{issue.Number}: {issue.Title}\n";
                    result += $"  State: {issue.State}";
                    if (issue.State == ItemState.Closed && issue.ClosedAt.HasValue)
                    {
                        result += $" (closed {issue.ClosedAt.Value:yyyy-MM-dd})";
                    }
                    result += "\n";
                    result += $"  Labels: {labels}\n";
                    result += $"  Assignees: {assignees}\n";
                    result += $"  Created: {issue.CreatedAt:yyyy-MM-dd HH:mm}\n";
                    result += $"  Updated: {issue.UpdatedAt:yyyy-MM-dd HH:mm}\n";
                    result += $"  URL: {issue.HtmlUrl}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub issues: {ex.Message}";
            }
        }
    }
}
