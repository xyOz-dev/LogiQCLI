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
            var inputBuilder = new StringBuilder();
            string result = "";
            DateTime lastKeyTime = DateTime.Now;
            bool isPasting = false;

            AnsiConsole.Live(CreateInputPanel(""))
                .AutoClear(true)
                .Start(ctx => {
                    ctx.UpdateTarget(CreateInputPanel(""));
                    while(true)
                    {
                        // Check if there are multiple keys available (indicates pasting)
                        if (System.Console.KeyAvailable)
                        {
                            var currentTime = DateTime.Now;
                            if ((currentTime - lastKeyTime).TotalMilliseconds < 50)
                            {
                                isPasting = true;
                            }
                            lastKeyTime = currentTime;
                        }
                        else
                        {
                            isPasting = false;
                        }

                        var key = System.Console.ReadKey(true);
                        
                        // Check for various submit combinations that work across platforms
                        bool shouldSubmit = false;
                        
                        if (key.Key == ConsoleKey.Enter)
                        {
                            // If we're pasting, don't treat Enter as submit - just add newline
                            if (isPasting)
                            {
                                inputBuilder.AppendLine();
                            }
                            // Ctrl+Enter to submit (works on all platforms)
                            else if (key.Modifiers == ConsoleModifiers.Control)
                            {
                                shouldSubmit = true;
                            }
                            // Shift+Enter to submit (Windows/Linux style)
                            else if (key.Modifiers == ConsoleModifiers.Shift)
                            {
                                shouldSubmit = true;
                            }
                            // Command+Enter on macOS (detected as Alt modifier in some terminals)
                            else if (key.Modifiers == ConsoleModifiers.Alt)
                            {
                                shouldSubmit = true;
                            }
                            // Double Enter: if the last characters are already newlines, submit
                            else if (inputBuilder.Length > 0 && inputBuilder.ToString().EndsWith(Environment.NewLine))
                            {
                                shouldSubmit = true;
                            }
                            // Plain Enter adds new line
                            else
                            {
                                inputBuilder.AppendLine();
                            }
                        }
                        else if (key.Key == ConsoleKey.Tab && !isPasting)
                        {
                            // Tab to submit (good alternative for macOS) - but not during paste
                            shouldSubmit = true;
                        }
                        else if (key.Key == ConsoleKey.Escape && !isPasting)
                        {
                            // ESC to submit as well (another alternative) - but not during paste
                            shouldSubmit = true;
                        }
                        else if (key.Key == ConsoleKey.Backspace)
                        {
                            if (inputBuilder.Length > 0)
                            {
                                // Handle backspace for newlines properly
                                var str = inputBuilder.ToString();
                                if (str.EndsWith(Environment.NewLine))
                                {
                                    inputBuilder.Length -= Environment.NewLine.Length;
                                }
                                else
                                {
                                    inputBuilder.Length--;
                                }
                            }
                        }
                        else if (key.Key == ConsoleKey.Tab && isPasting)
                        {
                            // If pasting, treat tab as regular character
                            inputBuilder.Append('\t');
                        }
                        else if (!char.IsControl(key.KeyChar))
                        {
                            inputBuilder.Append(key.KeyChar);
                        }
                        
                        if (shouldSubmit)
                        {
                            result = inputBuilder.ToString().TrimEnd();
                            break;
                        }
                        
                        ctx.UpdateTarget(CreateInputPanel(inputBuilder.ToString()));
                    }
                });
            
            return result;
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
            var contentMarkup = new Markup(
                string.IsNullOrEmpty(currentInput)
                ? "[dim]Start typing...[/]"
                : Markup.Escape(currentInput) + "[blink]_[/]"
            );

            var layout = new Rows(
                contentMarkup,
                new Align(new Markup("[dim](Enter for new line | Ctrl+Enter, Tab, or ESC to submit)[/]"), HorizontalAlignment.Right)
            );
                
            return new Panel(layout)
                .Header(new PanelHeader("[green]Your Message[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.FromHex("#00ff87"))
                .Padding(1, 0);
        }
    }
}
