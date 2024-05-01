using mhwiki.cli.WikiTasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace mhwiki.cli;

internal class WikiTaskExecutor
{
    private WikiSite _wikiSite = new WikiSite(
            wikiClient: new WikiClient
            {
                ClientUserAgent = "mhwiki.cli/0.1 (github.com/hymccord/mhwiki-tools)"
            },
            apiEndpoint: "https://mhwiki.hitgrab.com/wiki/api.php"
        );

    public WikiTaskExecutor()
    {
    }

    public async Task InitializeAsync()
    {
        await _wikiSite.Initialization;
        await _wikiSite.LoginAsync();
    }

    public async Task ExecuteTask(WikiTask task)
    {
        await task.Execute(_wikiSite);
    }
}
