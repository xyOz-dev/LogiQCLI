using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components.Objects
{
    public static class TableFormatter
    {
        public static void RenderTable(string title, IEnumerable<Dictionary<string, string>> data, Color? borderColor = null)
        {
            if (!data.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No {title.ToLower()} found.[/]");
                return;
            }

            var firstRow = data.First();
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(borderColor ?? Color.Grey)
                .Title($"[bold cyan]{title}[/]");

            foreach (var column in firstRow.Keys)
            {
                table.AddColumn($"[bold]{column}[/]");
            }
            foreach (var row in data)
            {
                var values = firstRow.Keys.Select(key => Markup.Escape(row.GetValueOrDefault(key, ""))).ToArray();
                table.AddRow(values);
            }

            var centeredTable = Align.Center(table);
            AnsiConsole.Write(centeredTable);
            AnsiConsole.WriteLine();
        }

        public static void RenderKeyValueTable(string title, Dictionary<string, string> data, Color? borderColor = null)
        {
            if (!data.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No {title.ToLower()} data available.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(borderColor ?? Color.Grey)
                .Title($"[bold cyan]{title}[/]")
                .AddColumn("[bold]Property[/]")
                .AddColumn("[bold]Value[/]");

            foreach (var kvp in data)
            {
                table.AddRow($"[cyan]{Markup.Escape(kvp.Key)}[/]", kvp.Value != null ? Markup.Escape(kvp.Value) : "[dim]Not set[/]");
            }

            var centeredTable = Align.Center(table);
            AnsiConsole.Write(centeredTable);
            AnsiConsole.WriteLine();
        }

        public static void RenderCommandsTable(IEnumerable<CommandTableRow> commands)
        {
            if (!commands.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No commands available.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Title("[bold cyan]Available Commands[/]")
                .AddColumn("[bold]Command[/]")
                .AddColumn("[bold]Category[/]")
                .AddColumn("[bold]Description[/]")
                .AddColumn("[bold]Tags[/]");

            foreach (var cmd in commands.OrderBy(c => c.Category).ThenBy(c => c.Name))
            {
                var commandName = $"[green]/{Markup.Escape(cmd.Name)}[/]";
                if (!string.IsNullOrEmpty(cmd.Alias))
                {
                    commandName += $" [dim]({Markup.Escape(cmd.Alias)})[/]";
                }

                var tags = cmd.Tags.Any() ? string.Join(", ", cmd.Tags) : "[dim]none[/]";

                table.AddRow(
                    commandName,
                    $"[yellow]{Markup.Escape(cmd.Category)}[/]",
                    cmd.Description != null ? Markup.Escape(cmd.Description) : "[dim]No description[/]",
                    $"[dim]{tags}[/]"
                );
            }

            var centeredTable = Align.Center(table);
            AnsiConsole.Write(centeredTable);
            AnsiConsole.WriteLine();
        }

        public static void RenderModesTable(IEnumerable<ModeTableRow> modes, string currentModeId)
        {
            if (!modes.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No modes available.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Title("[bold cyan]Available Modes[/]")
                .AddColumn("[bold]Mode ID[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Type[/]")
                .AddColumn("[bold]Tools[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var mode in modes.OrderBy(m => m.IsBuiltIn ? 0 : 1).ThenBy(m => m.Name))
            {
                var status = mode.Id == currentModeId ? "[green]Active[/]" : "[dim]Available[/]";
                var type = mode.IsBuiltIn ? "[cyan]Built-in[/]" : "[yellow]Custom[/]";

                table.AddRow(
                    $"[bold]{Markup.Escape(mode.Id)}[/]",
                    Markup.Escape(mode.Name),
                    type,
                    $"[dim]{mode.ToolCount}[/]",
                    status
                );
            }

            var centeredTable = Align.Center(table);
            AnsiConsole.Write(centeredTable);
            AnsiConsole.WriteLine();
        }

        public static void RenderApiKeysTable(IEnumerable<ApiKeyTableRow> apiKeys, string activeKeyNickname)
        {
            if (!apiKeys.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No API keys configured.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green)
                .Title("[bold cyan]Configured API Keys[/]")
                .AddColumn("[bold]Nickname[/]")
                .AddColumn("[bold]Key Preview[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var key in apiKeys)
            {
                var status = key.Nickname == activeKeyNickname ? "[green]Active[/]" : "[dim]Available[/]";
                
                table.AddRow(
                    $"[cyan]{Markup.Escape(key.Nickname)}[/]",
                    $"[dim]{Markup.Escape(key.ObfuscatedKey)}[/]",
                    status
                );
            }

            var centeredTable = Align.Center(table);
            AnsiConsole.Write(centeredTable);
            AnsiConsole.WriteLine();
        }
    }

    public class CommandTableRow
    {
        public string Name { get; set; } = "";
        public string? Alias { get; set; }
        public string Category { get; set; } = "";
        public string? Description { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class ModeTableRow
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsBuiltIn { get; set; }
        public int ToolCount { get; set; }
    }

    public class ApiKeyTableRow
    {
        public string Nickname { get; set; } = "";
        public string ObfuscatedKey { get; set; } = "";
    }
} 