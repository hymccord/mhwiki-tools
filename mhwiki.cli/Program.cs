using mhwiki.cli.WikiTasks;
using Nito.AsyncEx;

using Spectre.Console;

namespace mhwiki.cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        AnsiConsole.WriteLine("Here's where I would ask for username:password");
        AnsiConsole.WriteLine();

        var executor = new WikiTaskExecutor();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .StartAsync("Logging into mhwiki...", async (_) =>
            {
                await executor.InitializeAsync();
            });
        AnsiConsole.Clear();

        WikiTask task = AnsiConsole.Prompt(
            new SelectionPrompt<WikiTask>()
                .Title("What are in you interested in doing?")
                .PageSize(10)
                .AddChoices([
                    new AddMiceTask(),
                    new NoopTask(),
                    new ExitTask(),
                ]));

        AnsiConsole.Clear();
        await executor.ExecuteTask(task);
    }
}
