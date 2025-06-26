using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Infrastructure.ApiClients.Documentation;
using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.Documentation.Objects;

namespace LogiQCLI.Tools.Documentation
{
    [ToolMetadata("Documentation", Tags = new[] { "safe", "query" })]
    public class GetLibraryDocsTool : ITool
    {
        private const int DEFAULT_MINIMUM_TOKENS = 10000;
        private readonly DocumentationApiClient _documentationClient;

        public GetLibraryDocsTool(DocumentationApiClient documentationClient)
        {
            _documentationClient = documentationClient;
        }

        public override List<string> RequiredServices => new List<string> { "DocumentationApiClient" };

        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "get_library_docs",
                Description = "Fetches up-to-date documentation for a library. You must call 'resolve_library_id' first to obtain the exact library ID required to use this tool, UNLESS the user explicitly provides a library ID in the format '/org/project' or '/org/project/version' in their query.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        libraryId = new
                        {
                            type = "string",
                            description = "Exact library ID (e.g., '/mongodb/docs', '/vercel/next.js', '/supabase/supabase', '/vercel/next.js/v14.3.0-canary.87') retrieved from 'resolve_library_id' or directly from user query in the format '/org/project' or '/org/project/version'."
                        },
                        topic = new
                        {
                            type = "string",
                            description = "Topic to focus documentation on (e.g., 'hooks', 'routing')."
                        },
                        tokens = new
                        {
                            type = "integer",
                            description = $"Maximum number of tokens of documentation to retrieve (default: {DEFAULT_MINIMUM_TOKENS}). Higher values provide more context but consume more tokens."
                        }
                    },
                    Required = new[] { "libraryId" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<GetLibraryDocsArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.LibraryId))
                {
                    return "Error: Invalid arguments. Library ID is required.";
                }

                var tokens = arguments.Tokens ?? DEFAULT_MINIMUM_TOKENS;
                if (tokens < DEFAULT_MINIMUM_TOKENS)
                {
                    tokens = DEFAULT_MINIMUM_TOKENS;
                }

                var result = await _documentationClient.FetchDocumentationAsync(
                    arguments.LibraryId, 
                    tokens, 
                    arguments.Topic);

                if (result == null)
                {
                    return "Documentation not found or not finalized for this library. This might have happened because you used an invalid library ID. To get a valid library ID, use the 'resolve_library_id' with the package name you wish to retrieve documentation for.";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Error executing get_library_docs tool: {ex.Message}";
            }
        }
    }
} 