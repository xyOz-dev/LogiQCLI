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
    public class CommentOnPullRequestTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public CommentOnPullRequestTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "comment_on_github_pull_request",
                Description = "Adds a comment to an existing GitHub pull request. " +
                              "Requires GitHub authentication token to be configured. " +
                              "Use this tool to provide feedback, ask questions, or respond to pull requests.",
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
                            description = "Pull request number to comment on. " +
                                         "Must be an existing pull request number in the repository. " +
                                         "Example: 42"
                        },
                        comment = new
                        {
                            type = "string",
                            description = "Comment text to add to the pull request. " +
                                         "Supports GitHub markdown formatting. " +
                                         "Include relevant feedback, questions, or responses."
                        }
                    },
                    Required = new[] { "pullRequestNumber", "comment" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<CommentOnPullRequestArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0 || string.IsNullOrEmpty(arguments.Comment))
                {
                    return "Error: Invalid arguments. Pull request number and comment are required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var prComment = await _gitHubClient.AddPullRequestCommentAsync(
                    arguments.Owner, 
                    arguments.Repo, 
                    arguments.PullRequestNumber, 
                    arguments.Comment);

                return $"Successfully added comment to pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}\n" +
                       $"Comment ID: {prComment.Id}\n" +
                       $"Author: {prComment.User.Login}\n" +
                       $"Created: {prComment.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n" +
                       $"URL: {prComment.HtmlUrl}\n" +
                       $"Content preview: {(arguments.Comment.Length > 100 ? arguments.Comment.Substring(0, 100) + "..." : arguments.Comment)}";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error commenting on GitHub pull request: {ex.Message}";
            }
        }

        internal class CommentOnPullRequestArguments
        {
            [JsonPropertyName("owner")]
            public string? Owner { get; set; }

            [JsonPropertyName("repo")]
            public string? Repo { get; set; }

            [JsonPropertyName("pullRequestNumber")]
            public int PullRequestNumber { get; set; }

            [JsonPropertyName("comment")]
            public string? Comment { get; set; }
        }
    }
}
