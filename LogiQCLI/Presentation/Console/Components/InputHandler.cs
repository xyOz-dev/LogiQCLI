using System;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Components
{
    public class InputHandler
    {
        public async Task<string> GetInputAsync()
        {
            return await Task.Run(() => GetAdvancedInput());
        }

        private string GetAdvancedInput()
        {
            var panel = new Panel(new Markup("[dim]Type your message and press [green]Enter[/] to send[/]"))
                .Header("[green]New Message[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex("#00ff87"))
                .Padding(1, 0)
                .Expand();
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[green]>[/]")
                    .PromptStyle("green"));
        }

        public string GetSingleLineInput(string prompt)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[green]{prompt}[/]")
                    .PromptStyle("green")
                    .ShowChoices(false)
                    .ValidationErrorMessage("[red]Input cannot be empty[/]")
                    .Validate(input =>
                    {
                        return string.IsNullOrWhiteSpace(input) 
                            ? ValidationResult.Error("[red]Input cannot be empty[/]")
                            : ValidationResult.Success();
                    }));
        }

        public bool GetConfirmation(string message)
        {
            return AnsiConsole.Prompt(
                new ConfirmationPrompt($"[yellow]{message}[/]")
                {
                    Yes = 'y',
                    No = 'n',
                    ShowChoices = true,
                    ShowDefaultValue = true,
                    DefaultValue = true,
                    InvalidChoiceMessage = "[red]Please select a valid option[/]"
                });
        }

        public T GetChoice<T>(string title, params T[] choices) where T : notnull
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<T>()
                    .Title($"[cyan]{title}[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                    .AddChoices(choices)
                    .UseConverter(choice => $"[cyan]{choice}[/]")
                    .HighlightStyle(new Style(
                        foreground: Color.FromHex("#00ffff"),
                        background: Color.FromHex("#1a1a1a"),
                        decoration: Decoration.Bold)));
        }

        public string GetPassword(string prompt)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[yellow]{prompt}[/]")
                    .PromptStyle("yellow")
                    .Secret()
                    .ValidationErrorMessage("[red]Password cannot be empty[/]")
                    .Validate(input =>
                    {
                        return string.IsNullOrWhiteSpace(input) 
                            ? ValidationResult.Error("[red]Password cannot be empty[/]")
                            : ValidationResult.Success();
                    }));
        }

        public async Task<string> GetInputWithLiveDisplay()
        {
            var inputBuilder = new StringBuilder();
            
            await AnsiConsole.Live(CreateInputPanel(inputBuilder.ToString()))
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    await Task.Run(() =>
                    {
                        while (true)
                        {
                            var key = System.Console.ReadKey(true);
                            
                            if (key.Key == ConsoleKey.Enter)
                                break;
                            
                            if (key.Key == ConsoleKey.Backspace && inputBuilder.Length > 0)
                            {
                                inputBuilder.Length--;
                            }
                            else if (!char.IsControl(key.KeyChar))
                            {
                                inputBuilder.Append(key.KeyChar);
                            }
                            
                            ctx.UpdateTarget(CreateInputPanel(inputBuilder.ToString()));
                        }
                    });
                });
                
            return inputBuilder.ToString();
        }

        private Panel CreateInputPanel(string currentInput)
        {
            var content = string.IsNullOrEmpty(currentInput) 
                ? "[dim]Start typing...[/]" 
                : Markup.Escape(currentInput) + "[blink]_[/]";
                
            return new Panel(new Markup(content))
                .Header("[green]Your Message[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex("#00ff87"))
                .Padding(1, 0)
                .Expand();
        }
    }
}
