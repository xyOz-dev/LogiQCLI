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
    public class ListIssueCommentsTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListIssueCommentsTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_issue_comments",
                Description = "Lists all comments from a specific GitHub issue. " +
                              "Use this tool to read discussion threads and understand issue context before adding comments.",
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
                        issueNumber = new
                        {
                            type = "integer",
                            description = "Issue number to get comments from. " +
                                         "Must be an existing issue number in the repository. " +
                                         "Example: 42"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum number of comments to return. " +
                                         "Default: 20, Maximum: 100"
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
                var arguments = JsonSerializer.Deserialize<ListIssueCommentsArguments>(args);
                if (arguments == null || arguments.IssueNumber <= 0)
                {
                    return "Error: Invalid arguments. Issue number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var comments = await _gitHubClient.GetIssueCommentsAsync(arguments.Owner, arguments.Repo, arguments.IssueNumber);

                var limit = Math.Min(arguments.Limit ?? 20, 100);
                var limitedComments = comments.Take(limit).ToList();

                if (!limitedComments.Any())
                {
                    return $"No comments found on issue #{arguments.IssueNumber} in {arguments.Owner}/{arguments.Repo}.";
                }

                var result = $"Found {limitedComments.Count} comments on issue #{arguments.IssueNumber} in {arguments.Owner}/{arguments.Repo}:\n\n";

                foreach (var comment in limitedComments)
                {
                    result += $"Comment #{comment.Id}\n";
                    result += $"Author: {comment.User.Login}\n";
                    result += $"Created: {comment.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                    result += $"Updated: {comment.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                    result += $"URL: {comment.HtmlUrl}\n";
                    
                    var body = comment.Body ?? "";
                    if (body.Length > 300)
                    {
                        result += $"Content: {body.Substring(0, 300)}...\n";
                    }
                    else
                    {
                        result += $"Content: {body}\n";
                    }
                    
                    result += "\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub issue comments: {ex.Message}";
            }
        }
    }
}