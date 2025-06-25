using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.GitHub.Objects;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "update" })]
    public class CommentOnIssueTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public CommentOnIssueTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "comment_on_github_issue",
                Description = "Add comments to existing GitHub issues. Supports markdown formatting. Requires GitHub authentication token.",
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
                            description = "Issue number to comment on. Must be existing issue."
                        },
                        comment = new
                        {
                            type = "string",
                            description = "Comment text to add. Supports GitHub markdown formatting."
                        }
                    },
                    Required = new[] { "issueNumber", "comment" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<CommentOnIssueArguments>(args);
                if (arguments == null || arguments.IssueNumber <= 0 || string.IsNullOrEmpty(arguments.Comment))
                {
                    return "Error: Invalid arguments. Issue number and comment are required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var issueComment = await _gitHubClient.AddIssueCommentAsync(
                    arguments.Owner, 
                    arguments.Repo, 
                    arguments.IssueNumber, 
                    arguments.Comment);

                return $"Successfully added comment to issue #{arguments.IssueNumber} in {arguments.Owner}/{arguments.Repo}\n" +
                       $"Comment ID: {issueComment.Id}\n" +
                       $"Author: {issueComment.User.Login}\n" +
                       $"Created: {issueComment.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n" +
                       $"URL: {issueComment.HtmlUrl}\n" +
                       $"Content preview: {(arguments.Comment.Length > 100 ? arguments.Comment.Substring(0, 100) + "..." : arguments.Comment)}";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error commenting on GitHub issue: {ex.Message}";
            }
        }

    }
}