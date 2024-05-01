using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

internal class ExitTask : WikiTask
{
    internal override string TaskName => "Exit";

    internal override Task Execute(WikiSite site)
    {
        Environment.Exit(0);

        return Task.CompletedTask;
    }
}
