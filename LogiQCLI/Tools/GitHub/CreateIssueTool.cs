using System;
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
    [ToolMetadata("GitHub", Tags = new[] { "github", "create" })]
    public class CreateIssueTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new() { nameof(GitHubClientWrapper) };

        public CreateIssueTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "create_github_issue",
                Description = "Create GitHub issues with title, body, labels, and assignees. Requires GitHub authentication token.",
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
                        title = new
                        {
                            type = "string",
                            description = "Issue title. Must be descriptive and concise."
                        },
                        body = new
                        {
                            type = "string",
                            description = "Issue description body. Supports GitHub markdown. Include reproduction steps, expected vs actual behavior."
                        },
                        labels = new
                        {
                            type = "array",
                            description = "Array of label names. Labels must exist in repository. Example: ['bug', 'priority-high']",
                            items = new { type = "string" }
                        },
                        assignees = new
                        {
                            type = "array",
                            description = "Array of GitHub usernames to assign. Users must have repository access.",
                            items = new { type = "string" }
                        }
                    },
                    Required = new[] { "title" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<CreateIssueArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Title))
                {
                    return "Error: Invalid arguments. Title is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var newIssue = new NewIssue(arguments.Title)
                {
                    Body = arguments.Body ?? string.Empty
                };

                if (arguments.Labels != null && arguments.Labels.Length > 0)
                {
                    foreach (var label in arguments.Labels)
                    {
                        newIssue.Labels.Add(label);
                    }
                }

                if (arguments.Assignees != null && arguments.Assignees.Length > 0)
                {
                    foreach (var assignee in arguments.Assignees)
                    {
                        newIssue.Assignees.Add(assignee);
                    }
                }

                var issue = await _gitHubClient.CreateIssueAsync(arguments.Owner, arguments.Repo, newIssue);

                return $"Successfully created issue #{issue.Number}: {issue.Title}\n" +
                       $"URL: {issue.HtmlUrl}\n" +
                       $"State: {issue.State}\n" +
                       $"Created: {issue.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error creating GitHub issue: {ex.Message}";
            }
        }
    }
}
