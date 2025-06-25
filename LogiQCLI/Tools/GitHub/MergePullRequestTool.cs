using System;
using System.Collections.Generic;
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
    [ToolMetadata("GitHub", Tags = new[] { "github", "destructive" })]
    public class MergePullRequestTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public MergePullRequestTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "merge_github_pull_request",
                Description = "Merge GitHub pull requests using specified strategy (merge/squash/rebase). Requires authentication token and appropriate permissions.",
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
                            description = "Pull request number to merge. Must be existing, open PR."
                        },
                        commitTitle = new
                        {
                            type = "string",
                            description = "Title for merge commit. If not provided, GitHub generates default."
                        },
                        commitMessage = new
                        {
                            type = "string",
                            description = "Additional message for merge commit. Optional."
                        },
                        mergeMethod = new
                        {
                            type = "string",
                            description = "Merge strategy. Options: 'merge' (merge commit), 'squash' (squash all), 'rebase' (rebase commits). Default: 'merge'"
                        },
                        sha = new
                        {
                            type = "string",
                            description = "SHA that PR head must match. Optional security check to ensure PR unchanged since review."
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
                var arguments = JsonSerializer.Deserialize<MergePullRequestArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0)
                {
                    return "Error: Invalid arguments. Pull request number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var mergePullRequest = new MergePullRequest();

                if (!string.IsNullOrEmpty(arguments.CommitTitle))
                {
                    mergePullRequest.CommitTitle = arguments.CommitTitle;
                }

                if (!string.IsNullOrEmpty(arguments.CommitMessage))
                {
                    mergePullRequest.CommitMessage = arguments.CommitMessage;
                }

                if (!string.IsNullOrEmpty(arguments.MergeMethod))
                {
                    if (Enum.TryParse<PullRequestMergeMethod>(arguments.MergeMethod, true, out var mergeMethod))
                    {
                        mergePullRequest.MergeMethod = mergeMethod;
                    }
                    else
                    {
                        return $"Error: Invalid merge method '{arguments.MergeMethod}'. Must be 'merge', 'squash', or 'rebase'.";
                    }
                }

                if (!string.IsNullOrEmpty(arguments.Sha))
                {
                    mergePullRequest.Sha = arguments.Sha;
                }

                var mergeResult = await _gitHubClient.MergePullRequestAsync(
                    arguments.Owner, 
                    arguments.Repo, 
                    arguments.PullRequestNumber, 
                    mergePullRequest);

                var result = $"Successfully merged pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}\n";
                result += $"Merged: {mergeResult.Merged}\n";
                result += $"SHA: {mergeResult.Sha}\n";
                result += $"Message: {mergeResult.Message}\n";

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error merging GitHub pull request: {ex.Message}";
            }
        }
    }
}
