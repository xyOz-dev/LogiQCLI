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
                Description = "Merges a GitHub pull request using specified merge strategy. " +
                              "Requires GitHub authentication token to be configured and appropriate permissions. " +
                              "Use this tool to merge approved pull requests into the target branch.",
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
                            description = "Pull request number to merge. " +
                                         "Must be an existing, open pull request number. " +
                                         "Example: 42"
                        },
                        commitTitle = new
                        {
                            type = "string",
                            description = "Title for the merge commit. " +
                                         "If not provided, GitHub will generate a default title. " +
                                         "Example: 'Merge pull request #42 from feature/new-login'"
                        },
                        commitMessage = new
                        {
                            type = "string",
                            description = "Additional message for the merge commit. " +
                                         "Optional. Can include details about the changes merged."
                        },
                        mergeMethod = new
                        {
                            type = "string",
                            description = "Merge strategy to use. Options: 'merge', 'squash', 'rebase'. " +
                                         "Default: 'merge'. " +
                                         "'merge' creates a merge commit, " +
                                         "'squash' squashes all commits into one, " +
                                         "'rebase' rebases commits onto target branch."
                        },
                        sha = new
                        {
                            type = "string",
                            description = "SHA that pull request head must match for merge to succeed. " +
                                         "Optional security check to ensure PR hasn't changed since review. " +
                                         "If provided, must be the current head SHA of the PR."
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
