using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Models;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;
using LogiQCLI.Core.Models.Modes.Interfaces;

namespace LogiQCLI.Presentation.Console.Session
{
    public class ChatSession
    {
        private readonly List<Message> _messages;
        private readonly object _messageLock;
        private readonly IModeManager _modeManager;
        private readonly FileReadRegistry _fileReadRegistry;
        public string Model { get; set; }

        public ChatSession(string? model = null, IModeManager? modeManager = null, FileReadRegistry? fileReadRegistry = null)
        {
            _messages = new List<Message>();
            _messageLock = new object();
            _modeManager = modeManager!;
            _fileReadRegistry = fileReadRegistry ?? new FileReadRegistry();
            Model = model ?? "google/gemini-2.5-pro";
            InitializeSystemPrompt();
        }

        public void AddMessage(Message message)
        {
            lock (_messageLock)
            {
                _messages.Add(message);
            }
        }

        public Message[] GetMessages()
        {
            lock (_messageLock)
            {
                return _messages.ToArray();
            }
        }

        public void ClearHistory()
        {
            lock (_messageLock)
            {
                var systemMessage = _messages.Find(m => m.Role == "system");
                _messages.Clear();
                if (systemMessage != null)
                {
                    _messages.Add(systemMessage);
                }
                _fileReadRegistry.Clear();
            }
        }

        public int GetMessageCount()
        {
            lock (_messageLock)
            {
                return _messages.Count;
            }
        }

        public void UpdateSystemPrompt()
        {
            lock (_messageLock)
            {
                var existingSystemIndex = _messages.FindIndex(m => m.Role == "system");
                var newSystemPrompt = CreateSystemPrompt();
                var newSystemMessage = new Message { Role = "system", Content = newSystemPrompt };
                
                if (existingSystemIndex >= 0)
                {
                    _messages[existingSystemIndex] = newSystemMessage;
                }
                else
                {
                    _messages.Insert(0, newSystemMessage);
                }
            }
        }

        private void InitializeSystemPrompt()
        {
            var systemPrompt = CreateSystemPrompt();
            _messages.Add(new Message { Role = "system", Content = systemPrompt });
        }

        public string CreateSystemPrompt()
        {
            if (_modeManager != null)
            {
                var currentMode = _modeManager.GetCurrentMode();
                if (!string.IsNullOrEmpty(currentMode.SystemPrompt))
                {
                    return AppendEnvironmentDetails(currentMode.SystemPrompt);
                }
            }

            return AppendEnvironmentDetails(GetDefaultSystemPrompt());
        }

        private string AppendEnvironmentDetails(string basePrompt)
        {
            var workspace = System.IO.Directory.GetCurrentDirectory();
            var sb = new StringBuilder();
            
            sb.AppendLine(basePrompt);
            sb.AppendLine();
            
            AppendSystemInformation(sb);
            AppendDevelopmentEnvironment(sb);
            AppendProjectStructure(sb, workspace);

            return sb.ToString();
        }

        private void AppendSystemInformation(StringBuilder sb)
        {
            try
            {
                sb.AppendLine("== System Environment ==");
                sb.AppendLine($"Operating System: {GetDetailedOSInfo()}");
                sb.AppendLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");
                sb.AppendLine($"Machine Name: {System.Environment.MachineName}");
                sb.AppendLine($"User: {System.Environment.UserName}");
                sb.AppendLine($"Domain: {System.Environment.UserDomainName}");
                sb.AppendLine($"System Directory: {System.Environment.SystemDirectory}");
                sb.AppendLine();
            }
            catch (System.Exception ex)
            {
                sb.AppendLine("== System Environment ==");
                sb.AppendLine($"  (Error reading system info: {ex.Message})");
                sb.AppendLine();
            }
        }

        private void AppendDevelopmentEnvironment(StringBuilder sb)
        {
            try
            {
                sb.AppendLine("== Development Environment ==");
                
    
                var dotnetVersion = GetDotNetVersion();
                if (!string.IsNullOrEmpty(dotnetVersion))
                {
                    sb.AppendLine($".NET Version: {dotnetVersion}");
                }
                
    
                var gitInfo = GetGitInformation();
                if (!string.IsNullOrEmpty(gitInfo))
                {
                    sb.AppendLine($"Git: {gitInfo}");
                }
                
    
                var nodeInfo = GetNodeInformation();
                if (!string.IsNullOrEmpty(nodeInfo))
                {
                    sb.AppendLine($"Node.js: {nodeInfo}");
                }
                
    
                var pythonInfo = GetPythonInformation();
                if (!string.IsNullOrEmpty(pythonInfo))
                {
                    sb.AppendLine($"Python: {pythonInfo}");
                }
                
    
                var packageManagers = GetPackageManagerInfo();
                if (!string.IsNullOrEmpty(packageManagers))
                {
                    sb.AppendLine($"Package Managers: {packageManagers}");
                }
                
                sb.AppendLine();
            }
            catch (System.Exception ex)
            {
                sb.AppendLine("== Development Environment ==");
                sb.AppendLine($"  (Error reading dev environment: {ex.Message})");
                sb.AppendLine();
            }
        }

        private void AppendProjectStructure(StringBuilder sb, string workspace)
        {
            try
            {
                var allFiles = System.IO.Directory.GetFiles(
                    workspace,
                    "*",
                    System.IO.SearchOption.AllDirectories);

                var excludedPatterns = new[]
                {
                    $"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}.git{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}.vs{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}.vscode{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}node_modules{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}__pycache__{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}.pytest_cache{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}target{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}dist{System.IO.Path.DirectorySeparatorChar}",
                    $"{System.IO.Path.DirectorySeparatorChar}build{System.IO.Path.DirectorySeparatorChar}"
                };

                var filteredFiles = allFiles
                    .Where(f => !excludedPatterns.Any(p => f.Contains(p)))
                    .Select(f => System.IO.Path.GetRelativePath(workspace, f))
                    .OrderBy(f => f)
                    .ToList();

                sb.AppendLine("== Project File Structure ==");
                
                if (filteredFiles.Count > 0)
                {
                    var filesByExtension = filteredFiles
                        .GroupBy(f => System.IO.Path.GetExtension(f).ToLowerInvariant())
                        .OrderBy(g => g.Key)
                        .ToList();

                    var totalShown = 0;
                    foreach (var group in filesByExtension)
                    {
                        if (totalShown >= 100) break;
                        
                        var ext = string.IsNullOrEmpty(group.Key) ? "(no extension)" : group.Key;
                        var filesToShow = group.Take(Math.Min(20, 100 - totalShown)).ToList();
                        
                        sb.AppendLine($"  {ext} files ({group.Count()}):");
                        foreach (var file in filesToShow)
                        {
                            sb.AppendLine($"    {file}");
                            totalShown++;
                        }
                        
                        if (group.Count() > filesToShow.Count)
                        {
                            sb.AppendLine($"    ... and {group.Count() - filesToShow.Count} more {ext} files");
                        }
                    }
                    
                    if (filteredFiles.Count > 100)
                    {
                        sb.AppendLine($"  ... and {filteredFiles.Count - 100} more files total");
                    }
                }
                else
                {
                    sb.AppendLine("  (No files found)");
                }
            }
            catch (System.Exception ex)
            {
                sb.AppendLine("== Project File Structure ==");
                sb.AppendLine($"  (Error reading directory: {ex.Message})");
            }
        }

        private string GetDetailedOSInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var version = System.Environment.OSVersion.Version;
                var buildNumber = GetWindowsBuildNumber();
                return $"Windows {version.Major}.{version.Minor}.{version.Build}{buildNumber}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var distro = GetLinuxDistribution();
                return $"Linux {RuntimeInformation.OSDescription} {distro}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"macOS {RuntimeInformation.OSDescription}";
            }
            else
            {
                return RuntimeInformation.OSDescription;
            }
        }

        [SupportedOSPlatform("windows")]
        private string GetWindowsBuildNumber()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var buildLabEx = key?.GetValue("BuildLabEx")?.ToString();
                if (!string.IsNullOrEmpty(buildLabEx))
                {
                    return $" ({buildLabEx})";
                }
            }
            catch { }
            return "";
        }

        private string GetLinuxDistribution()
        {
            try
            {
                if (System.IO.File.Exists("/etc/os-release"))
                {
                    var lines = System.IO.File.ReadAllLines("/etc/os-release");
                    var pretty = lines.FirstOrDefault(l => l.StartsWith("PRETTY_NAME="));
                    if (pretty != null)
                    {
                        return pretty.Split('=')[1].Trim('"');
                    }
                }
            }
            catch { }
            return "";
        }

        private string GetDotNetVersion()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch { return ""; }
        }

        private string GetGitInformation()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch { return ""; }
        }

        private string GetNodeInformation()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch { return ""; }
        }

        private string GetPythonInformation()
        {
            try
            {
                var pythonCommands = new[] { "python3", "python" };
                foreach (var cmd in pythonCommands)
                {
                    try
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = cmd,
                                Arguments = "--version",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        var result = !string.IsNullOrEmpty(output) ? output : error;
                        if (!string.IsNullOrEmpty(result))
                        {
                            return result.Trim();
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return "";
        }

        private string GetPackageManagerInfo()
        {
            var managers = new List<string>();
            var commands = new[] 
            { 
                ("npm", "--version", "npm"),
                ("yarn", "--version", "Yarn"),
                ("pnpm", "--version", "pnpm"),
                ("pip", "--version", "pip"),
                ("cargo", "--version", "Cargo"),
                ("go", "version", "Go")
            };

            foreach (var (cmd, args, name) in commands)
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = cmd,
                            Arguments = args,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var version = output.Split('\n')[0].Trim();
                        managers.Add($"{name} ({version})");
                    }
                }
                catch { }
            }

            return string.Join(", ", managers);
        }

        private string DetectProjectType(string workspace)
        {
            var indicators = new Dictionary<string, string>
            {
                { "*.csproj", ".NET Project" },
                { "*.sln", ".NET Solution" },
                { "package.json", "Node.js Project" },
                { "requirements.txt", "Python Project" },
                { "Pipfile", "Python Project (Pipenv)" },
                { "pyproject.toml", "Python Project (Poetry/PEP 518)" },
                { "Cargo.toml", "Rust Project" },
                { "go.mod", "Go Project" },
                { "pom.xml", "Java Maven Project" },
                { "build.gradle", "Java Gradle Project" },
                { "composer.json", "PHP Composer Project" },
                { "Gemfile", "Ruby Project" },
                { "mix.exs", "Elixir Project" },
                { "deno.json", "Deno Project" }
            };

            var detectedTypes = new List<string>();
            
            foreach (var (pattern, type) in indicators)
            {
                try
                {
                    if (pattern.Contains("*"))
                    {
                        var files = System.IO.Directory.GetFiles(workspace, pattern, System.IO.SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            detectedTypes.Add(type);
                        }
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(workspace, pattern)))
                    {
                        detectedTypes.Add(type);
                    }
                }
                catch { }
            }

            return string.Join(", ", detectedTypes.Distinct());
        }

        private string GetDefaultSystemPrompt()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are LogiQ, an expert AI software-engineering assistant and development partner.");
            sb.AppendLine();
            
            sb.AppendLine("== Core Principles ==");
            sb.AppendLine("• **Efficiency First**: Use command chaining (e.g., `cmd1 && cmd2 && cmd3`) to minimize tool calls");
            sb.AppendLine("• **Context Awareness**: Leverage the comprehensive environment information provided");
            sb.AppendLine("• **Quality Focus**: Write clean, maintainable, well-documented code");
            sb.AppendLine("• **Safety**: Always validate inputs and handle edge cases gracefully");
            sb.AppendLine();
            
            sb.AppendLine("== Engineering Guidelines ==");
            sb.AppendLine("• **Code Style**: Follow existing patterns, naming conventions, and architecture");
            sb.AppendLine("• **Design Decisions**: Think through choices systematically; explain your reasoning clearly");
            sb.AppendLine("• **Error Handling**: Implement comprehensive error handling and validation");
            sb.AppendLine("• **Documentation**: Include clear comments and documentation for complex logic");
            sb.AppendLine("• **Testing**: Consider testability, generate comprehensive unit tests, and always run the tests you create using the correctly chained commands to ensure the tests always pass");
            sb.AppendLine("• **Performance**: Be mindful of performance implications and resource usage");
            sb.AppendLine();
            
            sb.AppendLine("== Communication Style ==");
            sb.AppendLine("• **Clarity**: Provide clear, actionable responses with specific examples");
            sb.AppendLine("• **Conciseness**: Be thorough but avoid unnecessary verbosity");
            sb.AppendLine("• **Proactive**: Anticipate potential issues and suggest improvements");
            sb.AppendLine("• **Questions**: Ask clarifying questions when requirements are ambiguous");
            sb.AppendLine();
            
            sb.AppendLine("== Tool Usage Best Practices ==");
            sb.AppendLine("• **Command Chaining**: Use platform-appropriate command chaining for efficiency");
            sb.AppendLine("  - Windows Example: `dotnet build ; dotnet run -test ; echo \"Complete\"`");
            sb.AppendLine("  - Unix Example: `dotnet build && dotnet run -test && echo \"Complete\"`");
            sb.AppendLine("• **File Operations**: Use relative paths when possible; respect project structure");
            sb.AppendLine("• **Parallel Execution**: Execute multiple read-only operations simultaneously when beneficial");
            sb.AppendLine();
            
            sb.AppendLine("== Problem-Solving Approach ==");
            sb.AppendLine("1. **Understand**: Analyze the request and gather extensive but necessary context");
            sb.AppendLine("2. **Plan**: Develop a clear strategy with specific, measurable steps");
            sb.AppendLine("3. **Execute**: Implement solutions systematically with proper validation");
            sb.AppendLine("4. **Verify**: Test functionality and ensure requirements are met");
            sb.AppendLine("5. **Document**: Explain changes and provide guidance for future maintenance unless specified otherwise.");
            sb.AppendLine();
            
            sb.AppendLine("IMPORTANT: You have access to comprehensive environment details, project structure,");
            sb.AppendLine("and development tools. Use this information to provide contextually appropriate");
            sb.AppendLine("solutions that work seamlessly in the current environment.");
            sb.AppendLine();
            
            sb.AppendLine("Remember: You are LogiQ - a trusted development partner. Act with the expertise");
            sb.AppendLine("and diligence of a senior software engineer, always prioritizing code quality,");
            sb.AppendLine("maintainability, and user success.");

            return sb.ToString();
        }
 
        public void RemoveMessage(Message message)
        {
            lock (_messageLock)
            {
                _messages.Remove(message);
            }
        }
    }
}
