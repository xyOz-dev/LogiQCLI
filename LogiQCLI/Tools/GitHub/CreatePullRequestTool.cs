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
    public class CreatePullRequestTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new() { nameof(GitHubClientWrapper) };

        public CreatePullRequestTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "create_github_pull_request",
                Description = "Create GitHub pull requests with title, body, source and target branches. Supports draft mode.",
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
                            description = "Pull request title. Must be descriptive and concise."
                        },
                        body = new
                        {
                            type = "string",
                            description = "PR description body. Supports GitHub markdown. Include changes made, why, and review notes."
                        },
                        head = new
                        {
                            type = "string",
                            description = "Source branch name (where changes come from). Example: 'feature/user-auth'"
                        },
                        baseRef = new
                        {
                            type = "string",
                            description = "Target branch name (where changes merge to). Typically 'main' or 'develop'. Default: 'main'"
                        },
                        draft = new
                        {
                            type = "boolean",
                            description = "Create as draft PR. Draft PRs cannot be merged until marked ready. Default: false"
                        }
                    },
                    Required = new[] { "title", "head" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<CreatePullRequestArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Title) || string.IsNullOrEmpty(arguments.Head))
                {
                    return "Error: Invalid arguments. Title and head branch are required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var newPullRequest = new NewPullRequest(arguments.Title, arguments.Head, arguments.BaseRef ?? "main")
                {
                    Body = arguments.Body ?? string.Empty,
                    Draft = arguments.Draft ?? false
                };

                var pullRequest = await _gitHubClient.CreatePullRequestAsync(arguments.Owner, arguments.Repo, newPullRequest);

                return $"Successfully created pull request #{pullRequest.Number}: {pullRequest.Title}\n" +
                       $"URL: {pullRequest.HtmlUrl}\n" +
                       $"State: {pullRequest.State}\n" +
                       $"Draft: {pullRequest.Draft}\n" +
                       $"From: {pullRequest.Head.Ref} → To: {pullRequest.Base.Ref}\n" +
                       $"Created: {pullRequest.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error creating GitHub pull request: {ex.Message}";
            }
        }
    }
}
