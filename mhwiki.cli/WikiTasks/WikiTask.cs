using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

internal abstract class WikiTask
{
    internal abstract string TaskName { get; }

    internal abstract Task Execute(WikiSite site);

    public override string ToString()
    {
        return TaskName;
    }
}
