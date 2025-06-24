using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class StreamingMessageRenderer
    {
        private readonly StringBuilder _buffer;
        private readonly object _lock;
        private bool _isFirstWrite;
        private readonly string _modelName;

        public StreamingMessageRenderer(string modelName = "ASSISTANT")
        {
            _buffer = new StringBuilder();
            _lock = new object();
            _isFirstWrite = true;
            _modelName = modelName;
        }

        public async Task BeginStreamingAsync()
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _isFirstWrite = true;
                }
            });
        }

        public void AppendContent(string content)
        {
            lock (_lock)
            {
                if (_isFirstWrite)
                {
                    _isFirstWrite = false;
                    AnsiConsole.MarkupLine("");
                    var centeredHeader = Align.Center(new Markup($"[#5f87ff]{_modelName.ToUpper()}:[/]"));
                    AnsiConsole.Write(centeredHeader);
                    AnsiConsole.WriteLine();
                }
                
                _buffer.Append(content);
                var processedContent = ApplyBasicFormatting(content);
                AnsiConsole.Write(processedContent);
            }
        }

        public async Task CompleteStreamingAsync()
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.WriteLine();
                }
            });
        }

        private string ApplyBasicFormatting(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var processed = Markup.Escape(content);
            return processed;
        }

        private string FormatContent(string content)
        {
            return MarkdownParser.ParseMarkdown(content);
        }

        private Panel CreateStreamingPanel()
        {
            return new Panel("[dim]Waiting for response...[/]")
                .Header($"[#5f87ff]{_modelName.ToUpper()}[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex("#5f87ff"))
                .Padding(1, 0, 1, 0)
                .Expand();
        }
    }
}
