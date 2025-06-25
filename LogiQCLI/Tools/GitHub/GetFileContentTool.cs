using System;
using System.Collections.Generic;
using System.Text;
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
    public class GetFileContentTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetFileContentTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_file_content",
                Description = "Get file content from GitHub repositories. Supports any branch/tag/commit reference. Displays files up to 1MB. Requires GitHub authentication token.",
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
                        path = new
                        {
                            type = "string",
                            description = "File path within repository. Example: 'src/main.js', 'README.md'"
                        },
                        reference = new
                        {
                            type = "string",
                            description = "Branch, tag, or commit SHA. Default: repository's default branch. Example: 'main', 'v1.0.0'"
                        }
                    },
                    Required = new[] { "path" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<GetFileContentArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path))
                {
                    return "Error: Invalid arguments. File path is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var fileContents = await _gitHubClient.GetFileContentAsync(arguments.Owner, arguments.Repo, arguments.Path);

                if (!fileContents.Any())
                {
                    return $"File not found: {arguments.Path}";
                }

                var file = fileContents[0];
                
                var result = $"File: {file.Path}\n";
                result += $"Type: {file.Type}\n";
                result += $"Size: {file.Size} bytes\n";
                result += $"SHA: {file.Sha}\n";
                result += $"Encoding: {file.Encoding}\n";
                result += $"URL: {file.HtmlUrl}\n\n";

                if (file.Type == "file")
                {
                    if (file.Size > 1000000)
                    {
                        result += "File is too large to display (>1MB). Use the download URL to access the content.";
                    }
                    else
                    {
                        try
                        {
                            var content = file.Encoding == "base64" 
                                ? Encoding.UTF8.GetString(Convert.FromBase64String(file.Content))
                                : file.Content;

                            if (content.Length > 10000)
                            {
                                result += $"Content (first 10,000 characters):\n";
                                result += $"```\n{content.Substring(0, 10000)}\n...\n```\n";
                                result += "\nFile content truncated. Use the download URL for complete content.";
                            }
                            else
                            {
                                result += $"Content:\n```\n{content}\n```";
                            }
                        }
                        catch (Exception ex)
                        {
                            result += $"Error decoding file content: {ex.Message}";
                        }
                    }
                }
                else
                {
                    result += "This is not a file (might be a directory or symlink).";
                }

                return result;
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub file content: {ex.Message}";
            }
        }
    }
}
