using Spectre.Console;

using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

internal class ToggleDebug : WikiTask
{
    internal override string TaskName => $"Debug: {(Debug ? "[green]on[/]" : "[red]off[/]")}";

    internal override Task Execute(WikiSite site)
    {
        AnsiConsole.MarkupLine($"""
            No changes will be made with debug on. Only log messages will be printed.
            Debug is currently {(Debug ? "[green]on[/]" : "[red]off[/]")}.
            """);
        if (AnsiConsole.Confirm("Do you want to toggle it?", defaultValue: false))
        {
            s_debug = !s_debug;
        }

        return Task.CompletedTask;
    }
}
