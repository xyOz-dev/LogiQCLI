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
    public class SearchCodeTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public SearchCodeTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "search_github_code",
                Description = "Searches for code patterns within GitHub repositories using GitHub's code search API. " +
                              "Use this tool to find specific functions, classes, patterns, or text across codebases.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "Search query using GitHub's code search syntax. " +
                                         "Supports language filters, file extensions, and complex patterns. " +
                                         "Example: 'function myFunction', 'class:User language:python', 'TODO extension:js'"
                        },
                        owner = new
                        {
                            type = "string",
                            description = "Repository owner to restrict search to. " +
                                         "Leave empty to search across all accessible repositories."
                        },
                        repo = new
                        {
                            type = "string",
                            description = "Repository name to restrict search to. " +
                                         "Requires owner to be specified as well."
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum number of search results to return. " +
                                         "Default: 10, Maximum: 50"
                        }
                    },
                    Required = new[] { "query" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<SearchCodeArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Query))
                {
                    return "Error: Invalid arguments. Search query is required.";
                }

                var searchResult = await _gitHubClient.SearchCodeAsync(
                    arguments.Query, 
                    arguments.Owner ?? "", 
                    arguments.Repo ?? "");

                var limit = Math.Min(arguments.Limit ?? 10, 50);
                var limitedResults = searchResult.Items.Take(limit).ToList();

                if (!limitedResults.Any())
                {
                    var searchScope = !string.IsNullOrEmpty(arguments.Owner) && !string.IsNullOrEmpty(arguments.Repo)
                        ? $" in {arguments.Owner}/{arguments.Repo}"
                        : "";
                    return $"No code found matching query '{arguments.Query}'{searchScope}.";
                }

                var result = $"Found {limitedResults.Count} code matches for '{arguments.Query}':\n\n";

                foreach (var item in limitedResults)
                {
                    result += $"File: {item.Name}\n";
                    result += $"Repository: {item.Repository.FullName}\n";
                    result += $"Path: {item.Path}\n";
                    result += $"SHA: {item.Sha}\n";
                    result += $"URL: {item.HtmlUrl}\n\n";
                }

                result += $"Total found: {searchResult.TotalCount} (showing {limitedResults.Count})";

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error searching GitHub code: {ex.Message}";
            }
        }
    }
}
