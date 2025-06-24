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
    public class GetPullRequestFilesTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public GetPullRequestFilesTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_github_pull_request_files",
                Description = "Gets a list of all files changed in a GitHub pull request with change statistics. " +
                              "Use this tool to see what files were modified, added, or deleted without the full diff content.",
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
                        pullRequestNumber = new
                        {
                            type = "integer",
                            description = "Pull request number to get files for. " +
                                         "Must be an existing pull request number in the repository. " +
                                         "Example: 42"
                        }
                    },
                    Required = new[] { "pullRequestNumber" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<GetPullRequestFilesArguments>(args);
                if (arguments == null || arguments.PullRequestNumber <= 0)
                {
                    return "Error: Invalid arguments. Pull request number is required.";
                }

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var files = await _gitHubClient.GetPullRequestFilesAsync(arguments.Owner, arguments.Repo, arguments.PullRequestNumber);

                if (!files.Any())
                {
                    return $"No files found for pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}.";
                }

                var result = $"Files changed in pull request #{arguments.PullRequestNumber} in {arguments.Owner}/{arguments.Repo}:\n\n";

                var totalAdditions = files.Sum(f => f.Additions);
                var totalDeletions = files.Sum(f => f.Deletions);
                var totalChanges = files.Sum(f => f.Changes);

                result += $"Summary: {files.Count} files changed, {totalAdditions} additions(+), {totalDeletions} deletions(-), {totalChanges} total changes\n\n";

                foreach (var file in files.OrderBy(f => f.FileName))
                {
                    result += $"File: {file.FileName}\n";
                    result += $"  Status: {file.Status}\n";
                    result += $"  Changes: +{file.Additions} -{file.Deletions} (total: {file.Changes})\n";
                    
                    if (!string.IsNullOrEmpty(file.PreviousFileName) && file.PreviousFileName != file.FileName)
                    {
                        result += $"  Previous name: {file.PreviousFileName}\n";
                    }

                    if (!string.IsNullOrEmpty(file.BlobUrl))
                    {
                        result += $"  View file: {file.BlobUrl}\n";
                    }

                    result += "\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error getting GitHub pull request files: {ex.Message}";
            }
        }
    }
}
