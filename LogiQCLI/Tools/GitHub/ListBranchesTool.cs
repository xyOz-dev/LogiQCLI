using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ListBranchesTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListBranchesTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_branches",
                Description = "List all branches from GitHub repository. Shows branch protection status. Requires GitHub authentication token.",
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
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum branches to return. Default: 50, Maximum: 100"
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
                var arguments = JsonSerializer.Deserialize<ListBranchesArguments>(args) ?? new ListBranchesArguments();

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var branches = await _gitHubClient.GetBranchesAsync(arguments.Owner, arguments.Repo);

                var limit = Math.Min(arguments.Limit ?? 50, 100);
                var limitedBranches = branches.Take(limit).ToList();

                if (!limitedBranches.Any())
                {
                    return $"No branches found in {arguments.Owner}/{arguments.Repo}.";
                }

                var result = $"Found {limitedBranches.Count} branches in {arguments.Owner}/{arguments.Repo}:\n\n";

                foreach (var branch in limitedBranches)
                {
                    result += $"Branch: {branch.Name}\n";
                    result += $"  SHA: {branch.Commit.Sha}\n";
                    result += $"  Protected: {branch.Protected}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub branches: {ex.Message}";
            }
        }

    }
}
