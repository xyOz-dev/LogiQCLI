using System;
using System.Collections.Generic;
using System.Linq;
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
    [ToolMetadata("GitHub", Tags = new[] { "github", "safe", "query" })]
    public class ListCommitsTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListCommitsTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_commits",
                Description = "List repository commits with filtering by branch, author, file path, or date range.",
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
                            description = "SHA or branch name for commits. Default: repository's default branch. Example: 'main', 'feature/new-feature'"
                        },
                        path = new
                        {
                            type = "string",
                            description = "Filter commits by file path. Only commits touching this path. Example: 'src/main.js'"
                        },
                        author = new
                        {
                            type = "string",
                            description = "Filter by author username or email. Example: 'john-doe' or 'john@example.com'"
                        },
                        since = new
                        {
                            type = "string",
                            description = "ISO 8601 date string for commits since. Example: '2023-01-01T00:00:00Z'"
                        },
                        until = new
                        {
                            type = "string",
                            description = "ISO 8601 date string for commits until. Example: '2023-12-31T23:59:59Z'"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum commits to return. Default: 30, Maximum: 100"
                        }
                    },
                    Required = new string[] { }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ListCommitsArguments>(args) ?? new ListCommitsArguments();

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var request = new CommitRequest();

                if (!string.IsNullOrEmpty(arguments.Sha))
                {
                    request.Sha = arguments.Sha;
                }

                if (!string.IsNullOrEmpty(arguments.Path))
                {
                    request.Path = arguments.Path;
                }

                if (!string.IsNullOrEmpty(arguments.Author))
                {
                    request.Author = arguments.Author;
                }

                if (!string.IsNullOrEmpty(arguments.Since) && DateTime.TryParse(arguments.Since, out var sinceDate))
                {
                    request.Since = sinceDate;
                }

                if (!string.IsNullOrEmpty(arguments.Until) && DateTime.TryParse(arguments.Until, out var untilDate))
                {
                    request.Until = untilDate;
                }

                var commits = await _gitHubClient.GetCommitsAsync(arguments.Owner, arguments.Repo, request);

                var limit = Math.Min(arguments.Limit ?? 30, 100);
                var limitedCommits = commits.Take(limit).ToList();

                if (!limitedCommits.Any())
                {
                    return $"No commits found in {arguments.Owner}/{arguments.Repo} with the specified criteria.";
                }

                var result = $"Found {limitedCommits.Count} commits in {arguments.Owner}/{arguments.Repo}:\n\n";

                foreach (var commit in limitedCommits)
                {
                    result += $"Commit: {commit.Sha[..7]}\n";
                    result += $"  Author: {commit.Commit.Author.Name} <{commit.Commit.Author.Email}>\n";
                    result += $"  Date: {commit.Commit.Author.Date:yyyy-MM-dd HH:mm:ss} UTC\n";
                    result += $"  Message: {commit.Commit.Message.Split('\n')[0]}\n";
                    if (commit.Commit.Message.Contains('\n'))
                    {
                        result += $"  (Multi-line message)\n";
                    }
                    result += $"  URL: {commit.HtmlUrl}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub commits: {ex.Message}";
            }
        }

    }
}
