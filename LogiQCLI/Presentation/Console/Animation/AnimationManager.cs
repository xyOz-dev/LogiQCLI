using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace LogiQCLI.Presentation.Console.Animation
{
    public class AnimationManager
    {
        public async Task ShowThinkingAnimationAsync(Func<Task> action)
        {
            await AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[cyan]Thinking...[/]", async ctx =>
                {
                    await action();
                });
        }

        public async Task ShowLoadingAnimationAsync(string message, Func<Task> action)
        {
            await AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Star)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync($"[cyan]{message}[/]", async ctx =>
                {
                    await action();
                });
        }

        public async Task ShowProgressAnimationAsync(string title, Func<ProgressTask, Task> action)
        {
            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[cyan]{title}[/]");
                    await action(task);
                });
        }

        public async Task<T> ShowLiveUpdateAsync<T>(string initialContent, Func<LiveDisplayContext, Task<T>> action)
        {
            T result = default(T);
            
            await AnsiConsole.Live(new Text(initialContent))
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Cropping(VerticalOverflowCropping.Top)
                .StartAsync(async ctx =>
                {
                    result = await action(ctx);
                });
                
            return result;
        }

        public void ShowSimpleSpinner(string message, Action action)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots8Bit)
                .SpinnerStyle(Style.Parse("cyan"))
                .Start($"[cyan]{message}[/]", ctx =>
                {
                    action();
                });
        }
    }
}
