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
    [ToolMetadata("GitHub", Tags = new[] { "github", "update" })]
    public class UpdateGitHubFileTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public UpdateGitHubFileTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "update_github_file",
                Description = "Updates an existing file in a GitHub repository with a commit message. " +
                              "Requires GitHub authentication token with write access. " +
                              "Use this tool to modify existing files directly in a repository.",
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
                            description = "File path within the repository to update. " +
                                         "Must be an existing file. " +
                                         "Example: 'src/main.js', 'docs/README.md', 'config/settings.json'"
                        },
                        content = new
                        {
                            type = "string",
                            description = "New file content to replace existing content completely. " +
                                         "Supports any text content including code, documentation, configuration, etc."
                        },
                        message = new
                        {
                            type = "string",
                            description = "Commit message describing the file update. " +
                                         "Example: 'Update configuration settings', 'Fix authentication bug'"
                        },
                        sha = new
                        {
                            type = "string",
                            description = "SHA of the file being updated. " +
                                         "Get this from get_github_file_content tool. " +
                                         "Required to prevent conflicts."
                        },
                        branch = new
                        {
                            type = "string",
                            description = "Branch name to update the file on. " +
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
                    Required = new[] { "path", "content", "message", "sha" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<UpdateFileArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path) || 
                    string.IsNullOrEmpty(arguments.Content) || string.IsNullOrEmpty(arguments.Message) ||
                    string.IsNullOrEmpty(arguments.Sha))
                {
                    return "Error: Invalid arguments. Path, content, commit message, and SHA are required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var updateRequest = new UpdateFileRequest(arguments.Message, arguments.Content, arguments.Sha, arguments.Branch ?? "main");
                
                if (!string.IsNullOrEmpty(arguments.AuthorName) && !string.IsNullOrEmpty(arguments.AuthorEmail))
                {
                    updateRequest.Author = new Committer(arguments.AuthorName, arguments.AuthorEmail, DateTimeOffset.UtcNow);
                }

                var result = await _gitHubClient.UpdateFileAsync(arguments.Owner, arguments.Repo, arguments.Path, updateRequest);

                var response = $"Successfully updated file {arguments.Path} in {arguments.Owner}/{arguments.Repo}\n";
                response += $"Commit SHA: {result.Commit.Sha}\n";
                response += $"Commit Message: {arguments.Message}\n";
                response += $"Branch: {arguments.Branch ?? "main"}\n";
                response += $"File URL: {result.Content.HtmlUrl}";

                return response;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error updating GitHub file: {ex.Message}";
            }
        }
    }
}
