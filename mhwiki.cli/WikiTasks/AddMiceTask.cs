using System.Net.Http.Json;
using System.Text.Json;
using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

partial class AddMiceTask : WikiTask
{
    private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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
                .StartAsync("Retrieving mice...", async (ctx) =>
                {
                    return await GetMiceAsync();
                });
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]Sorry, an error occurred getting mice from https://api.mouse.rip/mice");
            AnsiConsole.WriteException(e);

            return;
        }


        var choice = AnsiConsole.Prompt(
             new MultiSelectionPrompt<DisplayFunc>()
                 .Title("How would you like to check for missing mice?")
                 .PageSize(10)
                 .DefaultInstructionsText()
                 .AddChoices([
                     new DisplayFunc("Create category pages", async () => await CreateMissingCategoriesAsync(site, mice)),
                     new DisplayFunc("Create mouse group redirects", async () => await CreateMissingMouseGroupRedirects(site, mice)),
                     new DisplayFunc("Create individual mouse pages", async () => await CreateMissingMicePages(site, mice)),
                     new DisplayFunc("Update general mouse page", () => Task.CompletedTask),
                     new DisplayFunc("Go back", () => Task.CompletedTask)
                    ]));

        foreach (var task in choice)
        {
            await task.Invoke();
        }
    }

    

    private async Task CreateMissingMouseGroupRedirects(WikiSite site, Mouse[] mice)
    {
        List<string> missingGroups = await GetMissingPagesFor(site, mice, static (m) => m.Group).ToListAsync();

        List<string> selectedGroups = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Mouse groups with [red]missing[/] group pages. Select which ones you'd like to [green]create[/].")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a group," +
                    "[green]<enter>[/] to accept)[/]")
                .NotRequired()
                .AddChoices(missingGroups));

        if (selectedGroups.Count == 0)
        {
            AnsiConsole.WriteLine("None selected. Returning to previous menu");
        }

        AnsiConsole.MarkupLine($"You selected: {string.Join(", ", selectedGroups.Select(s => $"[green]{s}[/]"))}");
        AnsiConsole.MarkupLine("Here is the page template (with group placeholder text)");
        AnsiConsole.MarkupLine($"""

            [grey]{MouseGroupRedirectTemplate}[/]

            """);
        if (!AnsiConsole.Confirm("Create these pages?"))
        {
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Circle)
            .StartAsync("Creating pages", async (ctx) =>
            {
                foreach (var groupName in selectedGroups)
                {
                    var page = new WikiPage(site, "User:Xellis");// $"Category:{groupName}");
                    string pageContent = MouseGroupRedirectTemplate.Replace("GROUP_NAME_PLACEHOLDER", groupName);

                    await page.EditAsync(new WikiPageEditOptions()
                    {
                        Content = pageContent,
                        Summary = "Created page using mhwiki-tools"
                    });
                }
            });
    }

    private async Task CreateMissingMicePages(WikiSite site, Mouse[] mice)
    {
        var miceDict = mice.ToDictionary(m => m.AbbreviatedName);
        List<string> missingMice = await GetMissingPagesFor(site, mice, static (m) => m.AbbreviatedName).ToListAsync();

        var missingMiceByGroup = missingMice.Select(m => miceDict[m]).GroupBy(m => m.Group);

        MultiSelectionPrompt<string> prompt = new MultiSelectionPrompt<string>()
            .Title("[red]Missing[/] mice pages. Select which ones you'd like to [green]create[/].")
            .PageSize(20)
            .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a group," +
                "[green]<enter>[/] to accept)[/]")
            .NotRequired();

        foreach (var group in missingMiceByGroup)
        {
            prompt.AddChoiceGroup(group.Key, group.Select(m => m.AbbreviatedName));
        }

        missingMice = AnsiConsole.Prompt(prompt);

        if (missingMice.Count == 0)
        {
            AnsiConsole.WriteLine("None selected. Returning to previous menu");
            return;
        }

        AnsiConsole.MarkupLine($"""
            You selected: {string.Join(", ", missingMice.Select(s => $"[green]{s}[/]"))}
            Here is the page template [blue](with placeholder text)[/]

            [grey]{MouseGroupCategoryTemplate}[/]

            """);
        if (!AnsiConsole.Confirm("Create these pages?"))
        {
            AnsiConsole.Clear();
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync("[yellow]Creating pages[/]", async (ctx) =>
            {
                foreach (var groupName in missingMice)
                {
                    try
                    {
                        ctx.Status($"Creating {groupName}...");
                        await Task.Delay(3000);
                        AnsiConsole.MarkupLine($"Creating {groupName}... [green]Done![/]");

                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]Error![/]");
                        AnsiConsole.WriteException(ex);
                    }

                    //var page = new WikiPage(site, "User:Xellis");// $"Category:{groupName}");
                    //string pageContent = MouseGroupCategoryTemplate.Replace("GROUP_NAME_PLACEHOLDER", pageTitle);

                    //await page.EditAsync(new WikiPageEditOptions()
                    //{
                    //    Content = pageContent,
                    //    Summary = "Created page using mhwiki-tools"
                    //});
                }

                ctx.Status("Done!");
            });

        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine("Press enter to continue.");
        Console.ReadLine();
        AnsiConsole.Clear();
    }

    private static IAsyncEnumerable<string> GetMissingPagesFor(WikiSite site, Mouse[] mice, Func<Mouse, string> pageTitle)
    {
        return WikiPageStub.FromPageTitles(site, mice.Select(pageTitle).ToHashSet())
            .Where(w => w.IsMissing)
            .Select(w => w.Title!);
    }

    private async Task<Mouse[]> GetMiceAsync()
    {
        var response = await _restClient.GetFromJsonAsync<Mouse[]>("mice", s_serializerOptions);

        return response ?? [];
    }

    class DisplayFunc(string name, Func<Task> func)
    {
        public override string ToString() => name;

        public async Task Invoke() => await func();
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

        obj.InstructionsText = "[grey](Press [blue]<space>[/] to toggle a group," +
                "[green]<enter>[/] to accept)[/]";
        return obj;
    }
}
