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
    public class ListLabelsTool : ITool
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public override List<string> RequiredServices => new List<string> { "GitHubClientWrapper" };

        public ListLabelsTool(GitHubClientWrapper gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "list_github_labels",
                Description = "List all available labels from GitHub repository. Shows label names, colors, and descriptions for use with issues and pull requests.",
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
                var arguments = JsonSerializer.Deserialize<ListLabelsArguments>(args) ?? new ListLabelsArguments();

                if (string.IsNullOrEmpty(arguments.Owner) || string.IsNullOrEmpty(arguments.Repo))
                {
                    return "Error: Owner and repo are required. Configure default values or provide them explicitly.";
                }

                var labels = await _gitHubClient.GetLabelsAsync(arguments.Owner, arguments.Repo);

                if (!labels.Any())
                {
                    return $"No labels found in {arguments.Owner}/{arguments.Repo}.";
                }

                var result = $"Found {labels.Count} labels in {arguments.Owner}/{arguments.Repo}:\n\n";

                foreach (var label in labels.OrderBy(l => l.Name))
                {
                    result += $"Label: {label.Name}\n";
                    result += $"  Color: #{label.Color}\n";
                    result += $"  Description: {label.Description ?? "No description"}\n";
                    result += $"  Default: {label.Default}\n";
                    result += $"  URL: {label.Url}\n\n";
                }

                return result.TrimEnd();
            }
            catch (GitHubClientException ex)
            {
                return $"GitHub API Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub labels: {ex.Message}";
            }
        }
    }
}
