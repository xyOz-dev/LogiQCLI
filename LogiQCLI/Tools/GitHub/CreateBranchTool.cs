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
                Description = "Creates a new GitHub branch from an existing branch or commit. " +
                              "Requires GitHub authentication token to be configured. " +
                              "Use this tool to create feature branches, hotfix branches, or release branches.",
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
                        branchName = new
                        {
                            type = "string",
                            description = "Name for the new branch. " +
                                         "Must be valid Git branch name (no spaces, special chars). " +
                                         "Example: 'feature/new-login', 'hotfix/security-patch'"
                        },
                        fromBranch = new
                        {
                            type = "string",
                            description = "Source branch name to create the new branch from. " +
                                         "Default: repository's default branch (usually 'main' or 'master'). " +
                                         "Example: 'main', 'develop', 'release/v1.0'"
                        },
                        fromSha = new
                        {
                            type = "string",
                            description = "Specific commit SHA to create the branch from. " +
                                         "If provided, this takes precedence over fromBranch. " +
                                         "Must be a valid 40-character SHA hash."
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
