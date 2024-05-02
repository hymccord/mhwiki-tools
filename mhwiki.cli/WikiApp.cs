using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

using mhwiki.cli.WikiTasks;

using Spectre.Console;

using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace mhwiki.cli;

internal class WikiApp
{
    private readonly WikiClient _wikiClient;
    private readonly List<WikiTask> _tasks;

    public WikiApp()
    {
        _wikiClient = new WikiClient()
        {
            ClientUserAgent = "mhwiki.cli/0.1 (github.com/hymccord/mhwiki-tools)"
        };

        _tasks = [
            new AddMiceTask(),
        ];
    }

    public async Task RunAsync()
    {
        WikiSite site = await InitializeAsync();

        while (true)
        {
            AnsiConsole.Clear();
            WikiTask task = AnsiConsole.Prompt(
                new SelectionPrompt<WikiTask>()
                    .Title("What are in you interested in doing?")
                    .PageSize(10)
                    .AddChoices([
                        .._tasks,
                        new ExitTask(),
                    ]));

            await task.Execute(site);
        }

    }

    public async Task<WikiSite> InitializeAsync()
    {
        bool isLoggedIn = false;
        WikiSite? wikiSite = null;
        await AnsiConsole.Status()
            .StartAsync("[yellow]Logging in...[/]", async (ctx) =>
            {
                // MUST load cookies before creating site client
                await LoadSessionCookiesAsync();

                wikiSite = CreateSiteClient();

                // Initialize will call RefreshAccountInfoAsync so all we have to do is check for is IsUser;
                await wikiSite.Initialization;
                isLoggedIn = wikiSite.AccountInfo.IsUser;

            });

        if (wikiSite is null)
        {
            AnsiConsole.MarkupLine("[red]Something went very wrong...[/]");
            throw new InvalidOperationException();
        }

        int tries = 0;
        while (!isLoggedIn)
        {
            if (tries >= 3)
            {
                AnsiConsole.MarkupLine("""
                    [red]Login failed.

                    Too many attempts![/]
                    """);

                Environment.Exit(1);
            }

            if (tries == 0)
            {
                AnsiConsole.MarkupLine("MHWiki credentials are either [yellow]expired[/] or [red]missing[/].");
            }

            if (tries > 0)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"Login failed. Try again. ([red]{tries}/3[/])");
            }

            isLoggedIn = await LoginWithPromptAsync(wikiSite);
            tries++;

            if (isLoggedIn)
            {
                AnsiConsole.MarkupLine("[green]Success![/]");

                Task[] taskArray = [
                    Task.Delay(500),
                    SaveSessionCookiesAsync()
                ];

                await Task.WhenAll(taskArray);
            }
        }

        return wikiSite;
    }

    private WikiSite CreateSiteClient()
    {
        return new WikiSite(
            _wikiClient,
            apiEndpoint: "https://mhwiki.hitgrab.com/wiki/api.php"
        );
    }

    private async Task<bool> LoginWithPromptAsync(WikiSite wikiSite)
    {
        string username = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]username[/]:")
                .PromptStyle("yellow"));
        string password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]password[/]:")
                .PromptStyle("red")
                .Secret('*'));

        await AnsiConsole.Status()
            .StartAsync("[yellow]Logging in...[/]", async (ctx) =>
            {
                try
                {
                    await wikiSite!.LoginAsync(username, password);
                }
                catch { }
            });

        return wikiSite.AccountInfo.IsUser;
    }

    private async Task LoadSessionCookiesAsync()
    {
        if (!File.Exists("cookies.json"))
        {
            return;
        }

        using FileStream file = File.OpenRead("cookies.json");
        SavedCookie[]? cookies = null;
        try
        {
            cookies = await JsonSerializer.DeserializeAsync<SavedCookie[]>(file);
        }
        catch { }

        if (cookies is null)
        {
            return;
        }

        foreach (SavedCookie c in cookies)
        {
            _wikiClient.CookieContainer.Add(new Cookie(c.Name, c.Value, c.Path, c.Domain)
            {
                Expires = c.Expires,
            });
        }
    }

    private async Task SaveSessionCookiesAsync()
    {
        await AnsiConsole.Status().StartAsync("[grey]Storing cookies...[/]", async (ctx) =>
        {
            CookieCollection cookies = _wikiClient.CookieContainer.GetAllCookies();

            SavedCookie[] savedCookies = cookies
                .Select(token => new SavedCookie(token.Name, token.Value, token.Path, token.Domain, token.Expires))
                .ToArray();

            await File.WriteAllTextAsync("cookies.json", JsonSerializer.Serialize(savedCookies));
        });
    }

    private record SavedCookie(string Name, string Value, string Path, string Domain, DateTime Expires);

}
