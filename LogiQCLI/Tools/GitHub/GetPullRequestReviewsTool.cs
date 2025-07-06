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
    public class GetPullRequestReviewsTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetPullRequestReviewsTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_pull_request_reviews",
                Description = "Get all reviews for GitHub pull requests including reviewer comments, approval status, and change requests.",
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
                        pullRequestNumber = new
                        {
                            type = "integer",
                            description = "Pull request number to get reviews for. Must be existing PR."
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
                var arguments = JsonSerializer.Deserialize<GetPullRequestReviewsArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0)
                {
                    return "Error: Invalid arguments. Pull request number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var reviews = await _gitHubClient.GetPullRequestReviewsAsync(arguments.Owner, arguments.Repo, arguments.PullRequestNumber);

                if (!reviews.Any())
                {
                    return $"No reviews found for pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}.";
                }

                var result = $"Reviews for pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}:\n\n";

                var approvals = reviews.Count(r => r.State == "APPROVED");
                var requestChanges = reviews.Count(r => r.State == "CHANGES_REQUESTED");
                var comments = reviews.Count(r => r.State == "COMMENTED");

                result += $"Review Summary: {approvals} approvals, {requestChanges} change requests, {comments} comments\n\n";

                foreach (var review in reviews.OrderBy(r => r.SubmittedAt))
                {
                    result += $"Review by {review.User.Login}\n";
                    result += $"  State: {review.State}\n";
                    result += $"  Submitted: {review.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                    
                    if (!string.IsNullOrEmpty(review.Body))
                    {
                        var body = review.Body;
                        if (body.Length > 200)
                        {
                            result += $"  Comment: {body.Substring(0, 200)}...\n";
                        }
                        else
                        {
                            result += $"  Comment: {body}\n";
                        }
                    }

                    result += $"  URL: {review.HtmlUrl}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub pull request reviews: {ex.Message}";
            }
        }
    }
}
