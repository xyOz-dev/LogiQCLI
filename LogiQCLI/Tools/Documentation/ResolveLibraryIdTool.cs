using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.Documentation;
using LogiQCLI.Infrastructure.ApiClients.Documentation.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Documentation.Objects;

namespace LogiQCLI.Tools.Documentation
{
    [ToolMetadata("Documentation", Tags = new[] { "safe", "query" })]
    public class ResolveLibraryIdTool : ITool
    {
        private readonly DocumentationApiClient _documentationClient;

        public ResolveLibraryIdTool(DocumentationApiClient documentationClient)
        {
            _documentationClient = documentationClient;
        }

        public override List<string> RequiredServices => new List<string> { "DocumentationApiClient" };

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "resolve_library_id",
                Description = "Resolves a package/product name to a library ID and returns a list of matching libraries. You MUST call this function before 'get_library_docs' to obtain a valid library ID UNLESS the user explicitly provides a library ID in the format '/org/project' or '/org/project/version' in their query.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        libraryName = new
                        {
                            type = "string",
                            description = "Library name to search for and retrieve a library ID."
                        }
                    },
                    Required = new[] { "libraryName" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<ResolveLibraryIdArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.LibraryName))
                {
                    return "Error: Invalid arguments. Library name is required.";
                }

                var searchResponse = await _documentationClient.SearchLibrariesAsync(arguments.LibraryName);

                if (!string.IsNullOrEmpty(searchResponse.Error))
                {
                    return searchResponse.Error;
                }

                if (searchResponse.Results == null || !searchResponse.Results.Any())
                {
                    return "No documentation libraries found matching your query.";
                }

                var resultsText = FormatSearchResults(searchResponse);

                var sb = new StringBuilder();
                sb.AppendLine("Available Libraries (top matches):");
                sb.AppendLine();
                sb.AppendLine("Each result includes:");
                sb.AppendLine("- Library ID: Library-compatible identifier (format: /org/project)");
                sb.AppendLine("- Name: Library or package name");
                sb.AppendLine("- Description: Short summary");
                sb.AppendLine("- Code Snippets: Number of available code examples");
                sb.AppendLine("- Trust Score: Authority indicator");
                sb.AppendLine("- Versions: List of versions if available. Use one of those versions if and only if the user explicitly provides a version in their query.");
                sb.AppendLine();
                sb.AppendLine("For best results, select libraries based on name match, trust score, snippet coverage, and relevance to your use case.");
                sb.AppendLine();
                sb.AppendLine("----------");
                sb.AppendLine();
                sb.Append(resultsText);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error executing resolve_library_id tool: {ex.Message}";
            }
        }

        private string FormatSearchResults(SearchResponse searchResponse)
        {
            if (searchResponse.Results == null || !searchResponse.Results.Any())
            {
                return "No documentation libraries found matching your query.";
            }

            var formattedResults = searchResponse.Results.Select(FormatSearchResult);
            return string.Join("\n----------\n", formattedResults);
        }

        private string FormatSearchResult(SearchResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"- Title: {result.Title}");
            sb.AppendLine($"- Library ID: {result.Id}");
            sb.AppendLine($"- Description: {result.Description}");

            if (result.TotalSnippets != -1)
            {
                sb.AppendLine($"- Code Snippets: {result.TotalSnippets}");
            }

            if (result.TrustScore.HasValue && result.TrustScore != -1)
            {
                sb.AppendLine($"- Trust Score: {result.TrustScore}");
            }

            if (result.Versions != null && result.Versions.Length > 0)
            {
                sb.AppendLine($"- Versions: {string.Join(", ", result.Versions)}");
            }

            return sb.ToString().TrimEnd();
        }
    }
} 