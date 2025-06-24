using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using LogiQCLI.Tools.Core.Interfaces;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.SystemOperations.Objects;

namespace LogiQCLI.Tools.SystemOperations;

[ToolMetadata("SystemOperations", Tags = new[] { "system" })]
public class ExecuteCommandTool : ITool, IDisposable
{
    private static readonly ConcurrentDictionary<string, TerminalSession> ActiveSessions = new();
    private static readonly SemaphoreSlim SessionLock = new(1, 1);
    
    static ExecuteCommandTool()
    {
        AppDomain.CurrentDomain.ProcessExit += CleanupAllSessions;
        AppDomain.CurrentDomain.UnhandledException += (_, _) => CleanupAllSessions(null, null);
    }

    public override RegisteredTool GetToolInfo()
    {
        return new RegisteredTool
        {
            Name = "execute_command",
            Description = "Executes system commands in a shell environment with support for persistent terminal sessions. " +
                          "Use this tool for running CLI commands, scripts, build processes, or interactive programs. " +
                          "Supports both one-off commands and persistent sessions for maintaining state between commands. " +
                          "Automatically detects the platform (Windows/Unix) and uses the appropriate shell.",
            Parameters = new Parameters
            {
                Type = "object",
                Properties = new
                {
                    command = new
                    {
                        type = "string",
                        description = "Shell command to execute. " +
                                     "Examples: 'npm install', 'git status', 'python script.py'. " +
                                     "Required unless kill_session is true."
                    },
                    cwd = new
                    {
                        type = "string",
                        description = "Working directory for command execution. " +
                                     "Path relative to workspace or absolute. " +
                                     "Default: current workspace directory."
                    },
                    timeout = new
                    {
                        type = "integer",
                        description = "Maximum execution time in seconds before terminating. " +
                                     "Default: 60. Use higher values for long-running processes."
                    },
                    session_id = new
                    {
                        type = "string",
                        description = "Reuse an existing terminal session to maintain state/environment. " +
                                     "Useful for: activated virtual environments, changed directories, " +
                                     "or commands that depend on previous command results."
                    },
                    keep_alive = new
                    {
                        type = "boolean",
                        description = "Keep terminal session active after command for future reuse. " +
                                     "Default: false. Set true for interactive workflows."
                    },
                    kill_session = new
                    {
                        type = "boolean",
                        description = "Terminate the specified session to free resources. " +
                                     "Default: false. Requires session_id when true."
                    }
                },
                Required = Array.Empty<string>()
            }
        };
    }

    public override async Task<string> Execute(string args)
    {
        try
        {
            var arguments = JsonSerializer.Deserialize<ExecuteCommandArguments>(args);
            if (arguments == null)
            {
                return "Error: Invalid arguments.";
            }

            if (arguments.KillSession == true)
            {
                return await KillSession(arguments.SessionId);
            }

            if (string.IsNullOrWhiteSpace(arguments.Command))
            {
                return "Error: 'command' is required when not killing a session.";
            }

            if (!string.IsNullOrWhiteSpace(arguments.SessionId) || arguments.KeepAlive == true)
            {
                return await ExecuteInPersistentSession(arguments);
            }

            return await ExecuteInBackgroundProcess(arguments);
        }
        catch (JsonException ex)
        {
            return $"Error deserializing arguments: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Execution Error: {ex.Message}";
        }
    }

    private async Task<string> ExecuteInPersistentSession(ExecuteCommandArguments arguments)
    {
        await SessionLock.WaitAsync();
        try
        {
            var sessionId = arguments.SessionId ?? Guid.NewGuid().ToString("N");
            var session = ActiveSessions.GetOrAdd(sessionId, id => CreateNewSession(id, arguments.WorkingDirectory));

            if (!ProcessUtilities.IsProcessAlive(session.Process))
            {
                ActiveSessions.TryRemove(sessionId, out _);
                session.Dispose();
                session = CreateNewSession(sessionId, arguments.WorkingDirectory);
                ActiveSessions[sessionId] = session;
            }

            return await ExecuteCommandInSession(session, arguments.Command, arguments.Timeout ?? 60);
        }
        finally
        {
            SessionLock.Release();
        }
    }

    private TerminalSession CreateNewSession(string sessionId, string workingDirectory)
    {
        var shellInfo = ShellProvider.GetShellInfo();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = shellInfo.FileName,
            Arguments = shellInfo.PersistentArguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            var resolvedPath = Path.GetFullPath(workingDirectory);
            if (Directory.Exists(resolvedPath))
            {
                processStartInfo.WorkingDirectory = resolvedPath;
            }
        }

        var process = new Process { StartInfo = processStartInfo };
        process.Start();

        return new TerminalSession
        {
            Id = sessionId,
            Process = process,
            Input = process.StandardInput,
            Output = process.StandardOutput,
            Error = process.StandardError
        };
    }

    private async Task<string> ExecuteCommandInSession(TerminalSession session, string command, int timeout)
    {
        var outputBuilder = new StringBuilder();
        var completionMarker = $"__LOGIQ_COMMAND_COMPLETE_{Guid.NewGuid():N}__";
        
        var readTask = Task.Run(async () =>
        {
            var buffer = new char[4096];
            var markerFound = false;
            
            while (!markerFound)
            {
                var charsRead = await session.Output.ReadAsync(buffer, 0, buffer.Length);
                if (charsRead > 0)
                {
                    var chunk = new string(buffer, 0, charsRead);
                    outputBuilder.Append(chunk);
                    
                    if (outputBuilder.ToString().Contains(completionMarker))
                    {
                        markerFound = true;
                    }
                }
            }
        });

        await session.Input.WriteLineAsync(command);
        await session.Input.WriteLineAsync($"echo {completionMarker}");
        await session.Input.FlushAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        try
        {
            await readTask.WaitAsync(cts.Token);
        }
        catch (TaskCanceledException)
        {
            return $"Error: Command timed out after {timeout} seconds.";
        }

        var output = outputBuilder.ToString();
        var markerIndex = output.IndexOf(completionMarker);
        if (markerIndex >= 0)
        {
            output = output.Substring(0, markerIndex).TrimEnd();
        }

        return $"Session ID: {session.Id}\n{output}";
    }

    private async Task<string> KillSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return "Error: session_id is required to kill a session.";
        }

        if (ActiveSessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            return $"Session {sessionId} terminated successfully.";
        }

        return $"Error: Session {sessionId} not found.";
    }

    private async Task<string> ExecuteInBackgroundProcess(ExecuteCommandArguments arguments)
    {
        var shellInfo = ShellProvider.GetShellInfo();
        var processStartInfo = CreateProcessStartInfo(shellInfo, arguments);
        
        using var process = new Process { StartInfo = processStartInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(arguments.Timeout ?? 60));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (TaskCanceledException)
        {
            ProcessUtilities.TryKillProcess(process);
            return $"Error: Command timed out after {arguments.Timeout ?? 60} seconds.";
        }

        var error = errorBuilder.ToString().Trim();
        if (!string.IsNullOrEmpty(error) && process.ExitCode != 0)
        {
            return $"Error (Exit Code: {process.ExitCode}): {error}";
        }

        var output = outputBuilder.ToString().Trim();
        if (!string.IsNullOrEmpty(error))
        {
            output = string.IsNullOrEmpty(output) ? error : $"{output}\n\nWarnings:\n{error}";
        }

        return output;
    }


    private ProcessStartInfo CreateProcessStartInfo(ShellInfo shellInfo, ExecuteCommandArguments arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = shellInfo.FileName,
            Arguments = string.Format(shellInfo.ArgumentFormat, arguments.Command.Replace("\"", "\\\"")),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrWhiteSpace(arguments.WorkingDirectory))
        {
            var resolvedPath = Path.GetFullPath(arguments.WorkingDirectory);
            if (Directory.Exists(resolvedPath))
            {
                startInfo.WorkingDirectory = resolvedPath;
            }
            else
            {
                throw new DirectoryNotFoundException($"Working directory not found: {resolvedPath}");
            }
        }

        return startInfo;
    }


    private static void CleanupAllSessions(object sender, EventArgs e)
    {
        foreach (var session in ActiveSessions.Values)
        {
            session.Dispose();
        }
        ActiveSessions.Clear();
    }

    public void Dispose()
    {
        CleanupAllSessions(null, null);
        SessionLock?.Dispose();
    }

}
