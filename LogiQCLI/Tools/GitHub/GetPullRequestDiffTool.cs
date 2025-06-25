using System;
using System.Collections.Generic;
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
    public class GetPullRequestDiffTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetPullRequestDiffTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_pull_request_diff",
                Description = "Get complete diff content of GitHub pull requests showing all code changes. Large diffs are truncated based on maxLines parameter. Requires GitHub authentication token.",
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
                            description = "Pull request number to get diff for. Must be existing PR."
                        },
                        maxLines = new
                        {
                            type = "integer",
                            description = "Maximum diff lines to return. Large diffs truncated. Default: 1000, Maximum: 5000"
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
                var arguments = JsonSerializer.Deserialize<GetPullRequestDiffArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0)
                {
                    return "Error: Invalid arguments. Pull request number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var diff = await _gitHubClient.GetPullRequestDiffAsync(arguments.Owner, arguments.Repo, arguments.PullRequestNumber);

                if (string.IsNullOrEmpty(diff))
                {
                    return $"No diff content found for pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}.";
                }

                var maxLines = Math.Min(arguments.MaxLines ?? 1000, 5000);
                var lines = diff.Split('\n');

                var result = $"Diff for pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}:\n\n";

                if (lines.Length > maxLines)
                {
                    result += $"```diff\n{string.Join('\n', lines[..maxLines])}\n```\n\n";
                    result += $"Diff truncated: showing {maxLines} of {lines.Length} lines.\n";
                    result += "Use a higher maxLines value or view the full diff on GitHub for complete changes.";
                }
                else
                {
                    result += $"```diff\n{diff}\n```\n\n";
                    result += $"Complete diff: {lines.Length} lines total.";
                }

                return result;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub pull request diff: {ex.Message}";
            }
        }
    }
}
