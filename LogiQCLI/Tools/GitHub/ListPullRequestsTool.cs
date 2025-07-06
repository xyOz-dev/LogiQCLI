using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Octokit;

using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;
using LogiQCLI.Tools.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "safe", "query" })]
    public class ListPullRequestsTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListPullRequestsTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_pull_requests",
                Description = "List repository pull requests with filtering by state, branches, and sorting options.",
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
                            description = "PR state filter. Options: 'open', 'closed', 'all'. Default: 'open'"
                        },
                        head = new
                        {
                            type = "string",
                            description = "Filter by head branch name. Example: 'feature/new-feature'"
                        },
                        baseRef = new
                        {
                            type = "string",
                            description = "Filter by base branch name. Example: 'main', 'develop'"
                        },
                        sort = new
                        {
                            type = "string",
                            description = "Sort order. Options: 'created', 'updated', 'popularity', 'long-running'. Default: 'created'"
                        },
                        direction = new
                        {
                            type = "string",
                            description = "Sort direction. Options: 'asc', 'desc'. Default: 'desc'"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum PRs to return. Default: 30, Maximum: 100"
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
                var arguments = JsonSerializer.Deserialize<ListPullRequestsArguments>(args) ?? new ListPullRequestsArguments();

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var request = new PullRequestRequest();

                if (!string.IsNullOrEmpty(arguments.State))
                {
                    if (Enum.TryParse<ItemStateFilter>(arguments.State, true, out var stateFilter))
                    {
                        request.State = stateFilter;
                    }
                }

                if (!string.IsNullOrEmpty(arguments.Head))
                {
                    request.Head = arguments.Head;
                }

                if (!string.IsNullOrEmpty(arguments.BaseRef))
                {
                    request.Base = arguments.BaseRef;
                }

                if (!string.IsNullOrEmpty(arguments.Sort))
                {
                    if (Enum.TryParse<PullRequestSort>(arguments.Sort, true, out var sortOrder))
                    {
                        request.SortProperty = sortOrder;
                    }
                }

                if (!string.IsNullOrEmpty(arguments.Direction))
                {
                    if (Enum.TryParse<SortDirection>(arguments.Direction, true, out var sortDirection))
                    {
                        request.SortDirection = sortDirection;
                    }
                }

                var pullRequests = await _gitHubClient.GetPullRequestsAsync(arguments.Owner, arguments.Repo, request);

                var limit = Math.Min(arguments.Limit ?? 30, 100);
                var limitedPRs = pullRequests.Take(limit).ToList();

                if (!limitedPRs.Any())
                {
                    return $"No pull requests found in {arguments.Owner}/{arguments.Repo} with the specified criteria.";
                }

                var result = $"Found {limitedPRs.Count} pull requests in {arguments.Owner}/{arguments.Repo}:\n\n";

                foreach (var pr in limitedPRs)
                {
                    var labels = pr.Labels.Any() ? string.Join(", ", pr.Labels.Select(l => l.Name)) : "None";
                    var assignees = pr.Assignees.Any() ? string.Join(", ", pr.Assignees.Select(a => a.Login)) : "Unassigned";

                    result += $"#{pr.Number}: {pr.Title}\n";
                    result += $"  State: {pr.State}";
                    if (pr.State == ItemState.Closed && pr.ClosedAt.HasValue)
                    {
                        result += $" (closed {pr.ClosedAt.Value:yyyy-MM-dd})";
                    }
                    if (pr.MergedAt.HasValue)
                    {
                        result += $" - MERGED ({pr.MergedAt.Value:yyyy-MM-dd})";
                    }
                    result += "\n";
                    result += $"  Draft: {pr.Draft}\n";
                    result += $"  Author: {pr.User.Login}\n";
                    result += $"  Branch: {pr.Head.Ref} → {pr.Base.Ref}\n";
                    result += $"  Labels: {labels}\n";
                    result += $"  Assignees: {assignees}\n";
                    result += $"  Created: {pr.CreatedAt:yyyy-MM-dd HH:mm}\n";
                    result += $"  Updated: {pr.UpdatedAt:yyyy-MM-dd HH:mm}\n";
                    if (pr.Mergeable.HasValue)
                    {
                        result += $"  Mergeable: {pr.Mergeable.Value}\n";
                    }
                    result += $"  URL: {pr.HtmlUrl}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub pull requests: {ex.Message}";
            }
        }
    }
}
