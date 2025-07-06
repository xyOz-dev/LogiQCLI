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
    public class GetCommitDiffTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetCommitDiffTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_commit_diff",
                Description = "Get complete diff content of specific GitHub commits showing all code changes. Large diffs are truncated based on maxLines parameter.",
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
                        sha = new
                        {
                            type = "string",
                            description = "Commit SHA to get diff for. Must be valid commit SHA."
                        },
                        maxLines = new
                        {
                            type = "integer",
                            description = "Maximum diff lines to return. Large diffs truncated. Default: 1000, Maximum: 5000"
                        }
                    },
                    Required = new[] { "sha" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<GetCommitDiffArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Sha))
                {
                    return "Error: Invalid arguments. Commit SHA is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var commit = await _gitHubClient.GetCommitAsync(arguments.Owner, arguments.Repo, arguments.Sha);
                var diff = await _gitHubClient.GetCommitDiffAsync(arguments.Owner, arguments.Repo, arguments.Sha);

                if (string.IsNullOrEmpty(diff))
                {
                    return $"No diff content found for commit {arguments.Sha} in {arguments.Owner}/{arguments.Repo}.";
                }

                var maxLines = Math.Min(arguments.MaxLines ?? 1000, 5000);
                var lines = diff.Split('\n');

                var result = $"Diff for commit {arguments.Sha[..7]} in {arguments.Owner}/{arguments.Repo}:\n\n";
                result += $"Commit: {commit.Commit.Message.Split('\n')[0]}\n";
                result += $"Author: {commit.Commit.Author.Name} <{commit.Commit.Author.Email}>\n";
                result += $"Date: {commit.Commit.Author.Date:yyyy-MM-dd HH:mm:ss} UTC\n";
                result += $"Files changed: {commit.Files?.Count ?? 0}\n";
                
                if (commit.Stats != null)
                {
                    result += $"Additions: {commit.Stats.Additions}, Deletions: {commit.Stats.Deletions}\n";
                }
                
                result += "\n";

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
                return $"Error getting GitHub commit diff: {ex.Message}";
            }
        }
    }
}
