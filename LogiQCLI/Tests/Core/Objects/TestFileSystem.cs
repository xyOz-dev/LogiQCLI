using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogiQCLI.Tests.Core.Objects
{
    public class TestFileSystem : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly List<string> _createdFiles = new List<string>();
        private readonly List<string> _createdDirectories = new List<string>();

        public TestFileSystem()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "LogiQCLI_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _createdDirectories.Add(_tempDirectory);
        }

        public string CreateTempFile(string content, string fileName = null)
        {
            fileName = fileName ?? $"test_{Guid.NewGuid()}.txt";
            var filePath = Path.Combine(_tempDirectory, fileName);
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _createdDirectories.Add(directory);
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
            _createdFiles.Add(filePath);
            return filePath;
        }

        public string CreateLargeFile(int sizeInMB, string fileName = null)
        {
            fileName = fileName ?? $"large_{sizeInMB}MB_{Guid.NewGuid()}.txt";
            var filePath = Path.Combine(_tempDirectory, fileName);
            
            var chunkSize = 1024 * 1024;
            var chunk = new string('A', chunkSize);
            
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                for (int i = 0; i < sizeInMB; i++)
                {
                    writer.Write(chunk);
                }
            }
            
            _createdFiles.Add(filePath);
            return filePath;
        }

        public string CreateFileWithLineEndings(string content, string lineEndingType, string fileName = null)
        {
            fileName = fileName ?? $"lineending_{lineEndingType}_{Guid.NewGuid()}.txt";
            
            string processedContent;
            switch (lineEndingType.ToLowerInvariant())
            {
                case "crlf":
                    processedContent = content.Replace("\n", "\r\n");
                    break;
                case "lf":
                    processedContent = content.Replace("\r\n", "\n");
                    break;
                case "mixed":
                    var lines = content.Split('\n');
                    var sb = new StringBuilder();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        sb.Append(lines[i]);
                        if (i < lines.Length - 1)
                        {
                            sb.Append(i % 2 == 0 ? "\r\n" : "\n");
                        }
                    }
                    processedContent = sb.ToString();
                    break;
                default:
                    processedContent = content;
                    break;
            }
            
            return CreateTempFile(processedContent, fileName);
        }

        public string CreateBinaryFile(byte[] data, string fileName = null)
        {
            fileName = fileName ?? $"binary_{Guid.NewGuid()}.bin";
            var filePath = Path.Combine(_tempDirectory, fileName);
            
            File.WriteAllBytes(filePath, data);
            _createdFiles.Add(filePath);
            return filePath;
        }

        public string GetTestDataPath(string relativePath)
        {
            return Path.Combine(_tempDirectory, relativePath);
        }

        public string TempDirectory => _tempDirectory;

        public void CleanupTempFiles()
        {
            foreach (var file in _createdFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                }
            }

            foreach (var directory in _createdDirectories)
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }
                catch
                {
                }
            }

            _createdFiles.Clear();
            _createdDirectories.Clear();
        }

        public void Dispose()
        {
            CleanupTempFiles();
        }
    }
}