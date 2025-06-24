using System.Collections.Generic;
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
        public string Model { get; set; }

        public ChatSession(string? model = null, IModeManager? modeManager = null)
        {
            _messages = new List<Message>();
            _messageLock = new object();
            _modeManager = modeManager!;
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

        private string CreateSystemPrompt()
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

            var allFiles = System.IO.Directory.GetFiles(
                workspace,
                "*",
                System.IO.SearchOption.AllDirectories);

            var excludedPatterns = new[]
            {
                $"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}",
                $"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}"
            };

            var files = allFiles
                .Where(f => !excludedPatterns.Any(p => f.Contains(p)));

            var fileList = string.Join(
                "\n",
                files.Select(f =>
                {
                    var relative = System.IO.Path.GetRelativePath(workspace, f);
                    return relative;
                }));

            return $@"";
        }

        private string GetDefaultSystemPrompt()
        {
            return @"You are LogiQ, an expert AI software-engineering assistant.

== Engineering guidelines ==
- Follow existing code style, naming, and architecture.
- Think through design choices; explain your reasoning concisely.
- Handle edge cases and add appropriate error handling.
- If requirements are unclear, ask follow-up questions.
- Do not output tool commands unless you intend them to be executed.

Remember: you are LogiQ. Use the tools responsibly and act as a diligent senior engineer.";
        }
    }
}
