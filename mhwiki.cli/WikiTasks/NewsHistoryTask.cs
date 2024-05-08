using System.Net.Http.Headers;
using System.Text.Json;

using mhwiki.cli.Utililty;
using mhwiki.cli.Utililty.SpectreConsole;

using Spectre.Console;

using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
internal partial class NewsHistoryTask : WikiTask
{
    private readonly MouseHuntApiClient _apiClient;

    public NewsHistoryTask()
    {
        _apiClient = new MouseHuntApiClient();
    }

    internal override string TaskName => "Update News and History";

    internal override async Task Execute(WikiSite site)
    {
        AnsiConsole.MarkupLine("This module uses the MouseHunt through a desktop browser and can trigger a King's Reward.");

        Choice choice = AnsiConsole.Prompt(
            new SelectionPrompt<Choice>()
                .AddChoices([
                    new Choice("Update current event (not implemented)", UpdateCurrentEvent),
                    new Choice("Update patch notes", async () => await UpdatePatchNotes(site)),
                    new Choice("Go back", () => Task.CompletedTask)
                    ]
                )
            );

        await choice.Invoke();

        await Task.CompletedTask;
    }

    private async Task UpdateCurrentEvent()
    {
        await Task.CompletedTask;

        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        bool authenticated = false;
        await AnsiConsole.Status()
            .StartAsync("[yellow]Logging in to MouseHunt...[/]", async (ctx) =>
            {
                authenticated = await _apiClient.InitializeAsync();
            });

        if (authenticated)
        {
            return;
        }

        await LoginHelper.StartWithRetries("MouseHunt", _apiClient.LoginAsync);
    }
}
