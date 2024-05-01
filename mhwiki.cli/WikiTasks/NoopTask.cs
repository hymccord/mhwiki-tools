using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

internal class NoopTask : WikiTask
{
    internal override string TaskName => "No-op";

    internal override Task Execute(WikiSite site) => Task.CompletedTask;
}
