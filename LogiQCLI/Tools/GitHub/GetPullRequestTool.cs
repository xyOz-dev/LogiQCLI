using System;
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
    public class GetPullRequestTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetPullRequestTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_pull_request",
                Description = "Gets detailed information about a specific GitHub pull request including title, body, " +
                              "branches, reviewers, merge status, and metadata. " +
                              "Use this tool to analyze pull request details before taking action.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        owner = new
                        {
                            type = "string",
                            description = "Repository owner (username or organization name). " +
                                         "Required unless default owner is configured."
                        },
                        repo = new
                        {
                            type = "string",
                            description = "Repository name. " +
                                         "Required unless default repo is configured."
                        },
                        pullRequestNumber = new
                        {
                            type = "integer",
                            description = "Pull request number to retrieve. " +
                                         "Must be an existing pull request number in the repository. " +
                                         "Example: 42"
                        }
                    },
                    Required = new[] { "pullRequestNumber" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<GetPullRequestArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0)
                {
                    return "Error: Invalid arguments. Pull request number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var pr = await _gitHubClient.GetPullRequestAsync(arguments.Owner, arguments.Repo, arguments.PullRequestNumber);

                var result = $"Pull Request #{pr.Number}: {pr.Title}\n\n";
                result += $"Details:\n";
                result += $"  State: {pr.State}\n";
                result += $"  Draft: {pr.Draft}\n";
                result += $"  Author: {pr.User.Login}\n";
                result += $"  Created: {pr.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                result += $"  Updated: {pr.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";

                if (pr.ClosedAt.HasValue)
                {
                    result += $"  Closed: {pr.ClosedAt.Value:yyyy-MM-dd HH:mm:ss} UTC\n";
                }

                if (pr.MergedAt.HasValue)
                {
                    result += $"  Merged: {pr.MergedAt.Value:yyyy-MM-dd HH:mm:ss} UTC\n";
                    result += $"  Merged By: {pr.MergedBy?.Login ?? "Unknown"}\n";
                }

                result += $"  Branch: {pr.Head.Ref} → {pr.Base.Ref}\n";
                result += $"  Head SHA: {pr.Head.Sha}\n";
                result += $"  Base SHA: {pr.Base.Sha}\n";

                if (pr.Mergeable.HasValue)
                {
                    result += $"  Mergeable: {pr.Mergeable.Value}\n";
                }

                if (pr.Labels.Any())
                {
                    result += $"  Labels: {string.Join(", ", pr.Labels.Select(l => l.Name))}\n";
                }

                if (pr.Assignees.Any())
                {
                    result += $"  Assignees: {string.Join(", ", pr.Assignees.Select(a => a.Login))}\n";
                }

                if (pr.RequestedReviewers.Any())
                {
                    result += $"  Requested Reviewers: {string.Join(", ", pr.RequestedReviewers.Select(r => r.Login))}\n";
                }

                if (pr.Milestone != null)
                {
                    result += $"  Milestone: {pr.Milestone.Title}\n";
                }

                result += $"  Comments: {pr.Comments}\n";
                result += $"  Commits: {pr.Commits}\n";
                result += $"  Additions: {pr.Additions}\n";
                result += $"  Deletions: {pr.Deletions}\n";
                result += $"  Changed Files: {pr.ChangedFiles}\n";
                result += $"  URL: {pr.HtmlUrl}\n\n";

                result += $"Body:\n{pr.Body ?? "No description provided"}";

                return result;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub pull request: {ex.Message}";
            }
        }
    }
}
