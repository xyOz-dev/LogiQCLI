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
    public class CompareBranchesTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public CompareBranchesTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "compare_github_branches",
                Description = "Compare two branches or commits to see differences, file changes, and commit history. Shows additions, deletions, and changed files. Requires GitHub authentication token.",
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
                        baseRef = new
                        {
                            type = "string",
                            description = "Base branch/tag/commit SHA (old state). Example: 'main', 'v1.0.0'"
                        },
                        headRef = new
                        {
                            type = "string",
                            description = "Head branch/tag/commit SHA (new state with changes). Example: 'feature/new-feature', 'v1.1.0'"
                        }
                    },
                    Required = new[] { "baseRef", "headRef" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<CompareBranchesArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.BaseRef) || string.IsNullOrEmpty(arguments.HeadRef))
                {
                    return "Error: Invalid arguments. Both baseRef and headRef are required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var comparison = await _gitHubClient.CompareBranchesAsync(arguments.Owner, arguments.Repo, arguments.BaseRef, arguments.HeadRef);

                var result = $"Comparison: {arguments.BaseRef}...{arguments.HeadRef} in {arguments.Owner}/{arguments.Repo}\n\n";

                result += $"Status: {comparison.Status}\n";
                result += $"Ahead by: {comparison.AheadBy} commits\n";
                result += $"Behind by: {comparison.BehindBy} commits\n";
                result += $"Total commits: {comparison.TotalCommits}\n\n";

                if (comparison.Files != null && comparison.Files.Any())
                {
                    var totalAdditions = comparison.Files.Sum(f => f.Additions);
                    var totalDeletions = comparison.Files.Sum(f => f.Deletions);
                    var totalChanges = comparison.Files.Sum(f => f.Changes);

                    result += $"File Changes Summary:\n";
                    result += $"  Files changed: {comparison.Files.Count}\n";
                    result += $"  Total additions: {totalAdditions}\n";
                    result += $"  Total deletions: {totalDeletions}\n";
                    result += $"  Total changes: {totalChanges}\n\n";

                    result += $"Changed Files:\n";
                    foreach (var file in comparison.Files.Take(20))
                    {
                        result += $"  {file.Status}: {file.Filename} (+{file.Additions} -{file.Deletions})\n";
                    }

                    if (comparison.Files.Count > 20)
                    {
                        result += $"  ... and {comparison.Files.Count - 20} more files\n";
                    }
                    result += "\n";
                }

                if (comparison.Commits != null && comparison.Commits.Any())
                {
                    result += $"Recent Commits ({Math.Min(comparison.Commits.Count, 10)} of {comparison.Commits.Count}):\n";
                    foreach (var commit in comparison.Commits.Take(10))
                    {
                        result += $"  {commit.Sha[..7]}: {commit.Commit.Message.Split('\n')[0]}\n";
                        result += $"    Author: {commit.Commit.Author.Name} ({commit.Commit.Author.Date:yyyy-MM-dd})\n";
                    }

                    if (comparison.Commits.Count > 10)
                    {
                        result += $"  ... and {comparison.Commits.Count - 10} more commits\n";
                    }
                }

                result += $"\nComparison URL: {comparison.HtmlUrl}";

                return result;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error comparing GitHub branches: {ex.Message}";
            }
        }
    }
}
