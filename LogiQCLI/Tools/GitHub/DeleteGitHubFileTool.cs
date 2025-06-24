using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Octokit;

using LogiQCLI.Infrastructure.ApiClients.GitHub;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;
using LogiQCLI.Tools.GitHub.Objects;

namespace LogiQCLI.Tools.GitHub
{
    [ToolMetadata("GitHub", Tags = new[] { "github", "destructive" })]
    public class DeleteGitHubFileTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public DeleteGitHubFileTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "delete_github_file",
                Description = "Deletes an existing file from a GitHub repository with a commit message. " +
                              "Requires GitHub authentication token with write access. " +
                              "Use this tool to remove files directly from a repository.",
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
                        path = new
                        {
                            type = "string",
                            description = "File path within the repository to delete. " +
                                         "Must be an existing file. " +
                                         "Example: 'src/old-file.js', 'docs/deprecated.md'"
                        },
                        message = new
                        {
                            type = "string",
                            description = "Commit message describing the file deletion. " +
                                         "Example: 'Remove deprecated configuration', 'Delete unused test file'"
                        },
                        sha = new
                        {
                            type = "string",
                            description = "SHA of the file being deleted. " +
                                         "Get this from get_github_file_content tool. " +
                                         "Required to prevent conflicts."
                        },
                        branch = new
                        {
                            type = "string",
                            description = "Branch name to delete the file from. " +
                                         "Default: repository's default branch (usually 'main' or 'master')"
                        },
                        authorName = new
                        {
                            type = "string",
                            description = "Author name for the commit. " +
                                         "If not provided, uses the authenticated user's name."
                        },
                        authorEmail = new
                        {
                            type = "string",
                            description = "Author email for the commit. " +
                                         "If not provided, uses the authenticated user's email."
                        }
                    },
                    Required = new[] { "path", "message", "sha" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<DeleteFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path) || 
                    string.IsNullOrEmpty(arguments.Message) || string.IsNullOrEmpty(arguments.Sha))
                {
                    return "Error: Invalid arguments. Path, commit message, and SHA are required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var deleteRequest = new DeleteFileRequest(arguments.Message, arguments.Sha, arguments.Branch ?? "main");
                
                if (!string.IsNullOrEmpty(arguments.AuthorName) && !string.IsNullOrEmpty(arguments.AuthorEmail))
                {
                    deleteRequest.Author = new Committer(arguments.AuthorName, arguments.AuthorEmail, DateTimeOffset.UtcNow);
                }

                await _gitHubClient.DeleteFileAsync(arguments.Owner, arguments.Repo, arguments.Path, deleteRequest);

                var response = $"Successfully deleted file {arguments.Path} from {arguments.Owner}/{arguments.Repo}\n";
                response += $"Commit Message: {arguments.Message}\n";
                response += $"Branch: {arguments.Branch ?? "main"}";

                return response;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error deleting GitHub file: {ex.Message}";
            }
        }
    }
}
