using System.Net.Http.Json;
using System.Text.Json;

using mhwiki.cli.Utililty.SpectreConsole;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

partial class AddMiceTask : WikiTask
{
    private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
    private readonly HttpClient _restClient;

    public AddMiceTask()
    {
        _restClient = new HttpClient()
        {
            BaseAddress = new Uri("https://api.mouse.rip/"),
        };
    }

    internal override string TaskName => "Adding new mice (and related data)";

    internal override async Task Execute(WikiSite site)
    {

        Mouse[] mice;
        try
        {
            mice = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .StartAsync("Retrieving data from api.mouse.rip...", async (ctx) =>
                {
                    return await GetMiceAsync();
                });
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]Sorry, an error occurred getting mice from https://api.mouse.rip/mice[/]");
            AnsiConsole.WriteException(e);

            return;
        }


        var choice = AnsiConsole.Prompt(
             new MultiSelectionPrompt<Choice>()
                 .Title("How would you like to check for missing mice?")
                 .PageSize(10)
                 .DefaultInstructionsText()
                 .AddChoices([
                     new Choice("Create category (group + subgroup) pages", async () => await CreateMissingCategoriesAsync(site, mice)),
                     new Choice("Create mouse group redirects", async () => await CreateMissingMouseGroupRedirects(site, mice)),
                     new Choice("Create individual mouse pages", async () => await CreateMissingMicePages(site, mice)),
                     new Choice("Update general mouse page (add new rows)", () => Task.CompletedTask),
                     new Choice("Go back", () => Task.CompletedTask)
                    ]));

        foreach (var task in choice)
        {
            AnsiConsole.Clear();
            await task.Invoke();
        }
    }

    private static IAsyncEnumerable<string> GetMissingPagesFor(WikiSite site, IEnumerable<Mouse> mice, Func<Mouse, string> pageTitle)
    {
        return WikiPageStub.FromPageTitles(site, mice.Select(pageTitle).ToHashSet())
            .Where(w => w.IsMissing)
            .Select(w => w.Title!);
    }

    private void MarkupPageContent(string pageContent)
    {
        var p = new Panel($"[grey]{Markup.Escape(pageContent)}[/]")
        {
            Expand = true,
            Header = new PanelHeader("Page Content", Justify.Center)
        };
        AnsiConsole.Write(p);
    }

    private async Task<Mouse[]> GetMiceAsync()
    {
        var response = await _restClient.GetFromJsonAsync<Mouse[]>("mice", s_serializerOptions);

        return response ?? [];
    }
}

public static class MultiSelectionPromptPromptExtensions
{
    /// <summary>
    /// Sets the text that instructs the user of how to select items.
    /// </summary>
    /// <typeparam name="T">The prompt result type.</typeparam>
    /// <param name="obj">The prompt.</param>
    /// <param name="text">The text to display.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static MultiSelectionPrompt<T> DefaultInstructionsText<T>(this MultiSelectionPrompt<T> obj)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(obj);

        obj.InstructionsText = "[grey](Press [blue]<space>[/] to toggle a group, " +
                "[green]<enter>[/] to accept)[/]";
        return obj;
    }
}
