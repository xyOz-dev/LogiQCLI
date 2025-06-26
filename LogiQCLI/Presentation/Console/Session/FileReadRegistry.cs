using System;
using System.Collections.Concurrent;
using LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects;

namespace LogiQCLI.Presentation.Console.Session
{
    public class FileReadRegistry
    {
        private readonly ConcurrentDictionary<string, (string Hash, DateTime LastWriteUtc, long Length, Message MessageRef)> _entries = new();

        public bool TryGet(string path, out (string Hash, DateTime LastWriteUtc, long Length, Message MessageRef) entry)
            => _entries.TryGetValue(Normalize(path), out entry);

        public void Register(string path, string hash, DateTime lastWriteUtc, long length, Message message)
        {
            _entries[Normalize(path)] = (hash, lastWriteUtc, length, message);
        }

        public void Remove(string path)
        {
            _entries.TryRemove(Normalize(path), out _);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private static string Normalize(string path)
        {
            return System.IO.Path.GetFullPath(path).Replace('\\', '/').ToLowerInvariant();
        }
    }
} 