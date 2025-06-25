using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogiQCLI.Commands.Core.Interfaces;
using LogiQCLI.Commands.Core.Objects;

namespace LogiQCLI.Commands.Core
{
    public class CommandRegistry : ICommandRegistry
    {
        private readonly ConcurrentDictionary<string, CommandRegistrationEntry> _commands = new ConcurrentDictionary<string, CommandRegistrationEntry>(StringComparer.OrdinalIgnoreCase);

        public void RegisterCommand(CommandTypeInfo commandInfo)
        {
            if (commandInfo == null) throw new ArgumentNullException(nameof(commandInfo));
            
            var entry = new CommandRegistrationEntry
            {
                CommandInfo = commandInfo,
                Instance = null
            };
            
            _commands[commandInfo.Name] = entry;
            
            // Also register by alias if present
            if (!string.IsNullOrEmpty(commandInfo.Alias))
            {
                _commands[commandInfo.Alias] = entry;
            }
        }

        public void RegisterCommand(ICommand commandInstance)
        {
            if (commandInstance == null) throw new ArgumentNullException(nameof(commandInstance));
            
            var registeredInfo = commandInstance.GetCommandInfo();
            var commandType = commandInstance.GetType();
            
            CommandTypeInfo commandInfo;
            
            if (_commands.TryGetValue(registeredInfo.Name, out var existingEntry) && existingEntry.CommandInfo != null)
            {
                commandInfo = existingEntry.CommandInfo;
            }
            else
            {
                commandInfo = new CommandTypeInfo
                {
                    CommandType = commandType,
                    Name = registeredInfo.Name,
                    Category = commandInstance.Category,
                    Tags = new List<string>(commandInstance.Tags),
                    RequiredServices = new List<string>(commandInstance.RequiredServices),
                    Priority = commandInstance.Priority,
                    RequiresWorkspace = commandInstance.RequiresWorkspace,
                    Alias = registeredInfo.Alias
                };
            }
            
            var entry = new CommandRegistrationEntry
            {
                CommandInfo = commandInfo,
                Instance = commandInstance
            };
            
            _commands[commandInfo.Name] = entry;
            
            // Also register by alias if present
            if (!string.IsNullOrEmpty(commandInfo.Alias))
            {
                _commands[commandInfo.Alias] = entry;
            }
        }

        public ICommand? GetCommand(string name)
        {
            if (_commands.TryGetValue(name, out var entry))
            {
                return entry.Instance;
            }
            return null;
        }

        public CommandTypeInfo? GetCommandInfo(string name)
        {
            if (_commands.TryGetValue(name, out var entry))
            {
                return entry.CommandInfo;
            }
            return null;
        }

        public List<CommandTypeInfo> GetAllCommands()
        {
            return _commands.Values
                .Select(e => e.CommandInfo)
                .GroupBy(c => c.Name) // Remove duplicates from aliases
                .Select(g => g.First())
                .ToList();
        }

        public List<CommandTypeInfo> GetCommandsByCategory(string category)
        {
            return _commands.Values
                .Where(e => string.Equals(e.CommandInfo.Category, category, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.CommandInfo)
                .GroupBy(c => c.Name) // Remove duplicates from aliases
                .Select(g => g.First())
                .ToList();
        }

        public List<CommandTypeInfo> GetCommandsByTag(string tag)
        {
            return _commands.Values
                .Where(e => e.CommandInfo.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                .Select(e => e.CommandInfo)
                .GroupBy(c => c.Name) // Remove duplicates from aliases
                .Select(g => g.First())
                .ToList();
        }

        public List<CommandTypeInfo> QueryCommands(Func<CommandTypeInfo, bool> predicate)
        {
            return _commands.Values
                .Where(e => predicate(e.CommandInfo))
                .Select(e => e.CommandInfo)
                .GroupBy(c => c.Name) // Remove duplicates from aliases
                .Select(g => g.First())
                .ToList();
        }

        public bool IsCommandRegistered(string name)
        {
            return _commands.ContainsKey(name);
        }
    }
} 