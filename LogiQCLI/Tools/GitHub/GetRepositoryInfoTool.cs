using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.GitHub.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "safe", "query" })]
    public class GetRepositoryInfoTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetRepositoryInfoTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_repository_info",
                Description = "Get detailed repository information including metadata, statistics, configuration, and permissions. Requires GitHub authentication token.",
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
                var arguments = JsonSerializer.Deserialize<GetRepositoryInfoArguments>(args) ?? new GetRepositoryInfoArguments();

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var repository = await _gitHubClient.GetRepositoryAsync(arguments.Owner, arguments.Repo);

                var result = $"Repository Information: {repository.FullName}\n\n";

                result += $"Basic Information:\n";
                result += $"  Name: {repository.Name}\n";
                result += $"  Full Name: {repository.FullName}\n";
                result += $"  Description: {repository.Description ?? "No description"}\n";
                result += $"  Homepage: {repository.Homepage ?? "None"}\n";
                result += $"  URL: {repository.HtmlUrl}\n";
                result += $"  Clone URL: {repository.CloneUrl}\n";
                result += $"  SSH URL: {repository.SshUrl}\n\n";

                result += $"Repository Details:\n";
                result += $"  Owner: {repository.Owner.Login} ({repository.Owner.Type})\n";
                result += $"  Private: {repository.Private}\n";
                result += $"  Fork: {repository.Fork}\n";
                result += $"  Archived: {repository.Archived}\n";
                result += $"  Default Branch: {repository.DefaultBranch}\n";
                result += $"  Language: {repository.Language ?? "Not specified"}\n";
                result += $"  License: {repository.License?.Name ?? "No license"}\n\n";

                result += $"Statistics:\n";
                result += $"  Stars: {repository.StargazersCount:N0}\n";
                result += $"  Watchers: {repository.WatchersCount:N0}\n";
                result += $"  Forks: {repository.ForksCount:N0}\n";
                result += $"  Open Issues: {repository.OpenIssuesCount:N0}\n";
                result += $"  Size: {repository.Size:N0} KB\n\n";

                result += $"Dates:\n";
                result += $"  Created: {repository.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                result += $"  Updated: {repository.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                if (repository.PushedAt.HasValue)
                {
                    result += $"  Last Push: {repository.PushedAt.Value:yyyy-MM-dd HH:mm:ss} UTC\n";
                }

                result += "\nPermissions:\n";
                result += $"  Has Issues: {repository.HasIssues}\n";
                result += $"  Has Wiki: {repository.HasWiki}\n";
                result += $"  Has Pages: {repository.HasPages}\n";
                result += $"  Has Downloads: {repository.HasDownloads}\n";
                result += $"  Allow Merge Commit: {repository.AllowMergeCommit}\n";
                result += $"  Allow Squash Merge: {repository.AllowSquashMerge}\n";
                result += $"  Allow Rebase Merge: {repository.AllowRebaseMerge}\n";
                result += $"  Allow Auto Merge: {repository.AllowAutoMerge}\n";
                result += $"  Delete Branch on Merge: {repository.DeleteBranchOnMerge}\n";

                if (repository.Fork && repository.Parent != null)
                {
                    result += $"\nForked From:\n";
                    result += $"  Parent: {repository.Parent.FullName}\n";
                    result += $"  Parent URL: {repository.Parent.HtmlUrl}\n";
                }

                if (repository.Topics != null && repository.Topics.Count > 0)
                {
                    result += $"\nTopics: {string.Join(", ", repository.Topics)}\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub repository info: {ex.Message}";
            }
        }

    }
}
