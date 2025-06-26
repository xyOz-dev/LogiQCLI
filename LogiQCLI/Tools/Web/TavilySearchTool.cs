using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.Tavily;
using LogiQCLI.Infrastructure.ApiClients.Tavily.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Web.Objects;

namespace LogiQCLI.Tools.Web
{
    [ToolMetadata("Web", Tags = new[] { "search", "web", "research", "safe" })]
    public class TavilySearchTool : ITool
    {
        private readonly TavilyClient _tavilyClient;

        public override List<string> RequiredServices => new List<string> { "TavilyClient" };

        public TavilySearchTool(TavilyClient tavilyClient)
        {
            _tavilyClient = tavilyClient ?? throw new ArgumentNullException(nameof(tavilyClient));
        }

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "tavily_search",
                Description = "Search the web using Tavily Search API, optimized for LLMs and RAG. Provides comprehensive, factual search results with optional answers and follow-up questions.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The search query to execute. Be specific and include relevant keywords."
                        },
                        search_depth = new
                        {
                            type = "string",
                            description = "Search depth level. Options: 'basic' (faster, fewer sources) or 'advanced' (slower, more comprehensive)",
                            @enum = new[] { "basic", "advanced" }
                        },
                        max_results = new
                        {
                            type = "integer",
                            description = "Maximum number of search results to return (1-20, default: 5)",
                            minimum = 1,
                            maximum = 20
                        },
                        include_answer = new
                        {
                            type = "boolean",
                            description = "Whether to include a direct answer to the query based on search results (default: true)"
                        },
                        include_images = new
                        {
                            type = "boolean",
                            description = "Whether to include relevant images in the response (default: false)"
                        },
                        include_raw_content = new
                        {
                            type = "boolean",
                            description = "Whether to include raw HTML content of the pages (default: false)"
                        },
                        include_domains = new
                        {
                            type = "array",
                            description = "List of domains to include in search (optional)",
                            items = new { type = "string" }
                        },
                        exclude_domains = new
                        {
                            type = "array",
                            description = "List of domains to exclude from search (optional)",
                            items = new { type = "string" }
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
                var arguments = JsonSerializer.Deserialize<TavilySearchArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Query))
                {
                    return "Error: Invalid arguments. Query is required.";
                }

                var request = new TavilySearchRequest
                {
                    Query = arguments.Query,
                    SearchDepth = arguments.SearchDepth ?? "basic",
                    MaxResults = Math.Max(1, Math.Min(20, arguments.MaxResults ?? 5)),
                    IncludeAnswer = arguments.IncludeAnswer ?? true,
                    IncludeImages = arguments.IncludeImages ?? false,
                    IncludeRawContent = arguments.IncludeRawContent ?? false,
                    IncludeDomains = arguments.IncludeDomains,
                    ExcludeDomains = arguments.ExcludeDomains
                };

                var response = await _tavilyClient.SearchAsync(request);

                return FormatSearchResults(response);
            }
            catch (TavilyException ex)
            {
                return $"Error executing Tavily search: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error executing tavily_search tool: {ex.Message}";
            }
        }

        private string FormatSearchResults(TavilySearchResponse response)
        {
            var result = new List<string>();

            result.Add($"Search Query: {response.Query}");
            result.Add($"Response Time: {response.ResponseTime:F2}s");
            result.Add("");

            if (!string.IsNullOrEmpty(response.Answer))
            {
                result.Add("**Direct Answer:**");
                result.Add(response.Answer);
                result.Add("");
            }

            if (response.Results.Any())
            {
                result.Add("**Search Results:**");
                for (int i = 0; i < response.Results.Count; i++)
                {
                    var searchResult = response.Results[i];
                    result.Add($"{i + 1}. **{searchResult.Title}**");
                    result.Add($"   URL: {searchResult.Url}");
                    if (searchResult.Score > 0)
                    {
                        result.Add($"   Relevance Score: {searchResult.Score:F2}");
                    }
                    if (!string.IsNullOrEmpty(searchResult.PublishedDate))
                    {
                        result.Add($"   Published: {searchResult.PublishedDate}");
                    }
                    result.Add($"   Content: {searchResult.Content}");
                    
                    if (!string.IsNullOrEmpty(searchResult.RawContent))
                    {
                        result.Add($"   Raw Content: {searchResult.RawContent}");
                    }
                    result.Add("");
                }
            }

            if (response.Images?.Any() == true)
            {
                result.Add("**Related Images:**");
                for (int i = 0; i < response.Images.Count; i++)
                {
                    var image = response.Images[i];
                    result.Add($"{i + 1}. {image.Url}");
                    if (!string.IsNullOrEmpty(image.Description))
                    {
                        result.Add($"   Description: {image.Description}");
                    }
                }
                result.Add("");
            }

            if (response.FollowUpQuestions?.Any() == true)
            {
                result.Add("**Follow-up Questions:**");
                foreach (var question in response.FollowUpQuestions)
                {
                    result.Add($"- {question}");
                }
                result.Add("");
            }

            return string.Join("\n", result);
        }
    }
} 