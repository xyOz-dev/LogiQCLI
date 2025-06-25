using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Commands.Configuration
{
    public class WorkspaceCommandArguments
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }

    [CommandMetadata("Configuration", Tags = new[] { "config", "workspace" })]
    public class WorkspaceCommand : ICommand
    {
        public override RegisteredCommand GetCommandInfo()
        {
            return new RegisteredCommand
            {
                Name = "workspace",
                Description = "View or change the current workspace directory",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "New workspace path (optional - omit to view current workspace)"
                        }
                    }
                }
            };
        }

        public override Task<string> Execute(string args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    var currentWorkspace = Directory.GetCurrentDirectory();
                    return Task.FromResult($"[yellow]Current workspace: {currentWorkspace}[/]");
                }

                WorkspaceCommandArguments? arguments = null;
                
                // Try to parse as JSON first
                try
                {
                    arguments = JsonSerializer.Deserialize<WorkspaceCommandArguments>(args);
                }
                catch
                {
                    // If JSON parsing fails, treat the entire args as the path
                    arguments = new WorkspaceCommandArguments { Path = args.Trim() };
                }

                if (arguments?.Path != null)
                {
                    var newWorkspace = arguments.Path.Trim();
                    
                    if (!Directory.Exists(newWorkspace))
                    {
                        return Task.FromResult($"[red]Directory does not exist: {newWorkspace}[/]");
                    }
                    
                    try
                    {
                        Directory.SetCurrentDirectory(newWorkspace);
                        return Task.FromResult($"[green]Workspace changed to: {newWorkspace}[/]");
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult($"[red]Failed to change workspace: {ex.Message}[/]");
                    }
                }

                var currentDir = Directory.GetCurrentDirectory();
                return Task.FromResult($"[yellow]Current workspace: {currentDir}[/]");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"[red]Error: {ex.Message}[/]");
            }
        }
    }
} 