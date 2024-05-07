using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

internal abstract class WikiTask
{
    protected static bool s_debug = true;

    internal abstract string TaskName { get; }

    internal bool Debug => s_debug;

    internal abstract Task Execute(WikiSite site);

    public override string ToString()
    {
        return TaskName;
    }
}
