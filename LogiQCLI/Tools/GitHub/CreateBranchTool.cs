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
    [ToolMetadata("GitHub", Tags = new[] { "github", "create" })]
    public class CreateBranchTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public CreateBranchTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "create_github_branch",
                Description = "Create new GitHub branches from existing branches or specific commits. Requires GitHub authentication token with write access.",
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
                        branchName = new
                        {
                            type = "string",
                            description = "Name for new branch. Must be valid Git branch name. Example: 'feature/new-login', 'hotfix/security-patch'"
                        },
                        fromBranch = new
                        {
                            type = "string",
                            description = "Source branch name. Default: repository's default branch. Example: 'main', 'develop'"
                        },
                        fromSha = new
                        {
                            type = "string",
                            description = "Specific commit SHA to branch from. Takes precedence over fromBranch. Must be 40-character SHA."
                        }
                    },
                    Required = new[] { "branchName" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<CreateBranchArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.BranchName))
                {
                    return "Error: Invalid arguments. Branch name is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                string targetSha;

                if (!string.IsNullOrEmpty(arguments.FromSha))
                {
                    if (arguments.FromSha.Length != 40)
                    {
                        return "Error: fromSha must be a valid 40-character SHA hash.";
                    }
                    targetSha = arguments.FromSha;
                }
                else
                {
                    var fromBranch = arguments.FromBranch;
                    if (string.IsNullOrEmpty(fromBranch))
                    {
                        var repository = await _gitHubClient.GetRepositoryAsync(arguments.Owner, arguments.Repo);
                        fromBranch = repository.DefaultBranch;
                    }

                    var reference = await _gitHubClient.GetReferenceAsync(arguments.Owner, arguments.Repo, $"heads/{fromBranch}");
                    targetSha = reference.Object.Sha;
                }

                var newBranch = await _gitHubClient.CreateBranchAsync(arguments.Owner, arguments.Repo, arguments.BranchName, targetSha);

                return $"Successfully created branch '{arguments.BranchName}' in {arguments.Owner}/{arguments.Repo}\n" +
                       $"Branch reference: {newBranch.Ref}\n" +
                       $"Target SHA: {newBranch.Object.Sha}\n" +
                       $"URL: {newBranch.Url}";
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error creating GitHub branch: {ex.Message}";
            }
        }
    }
}
