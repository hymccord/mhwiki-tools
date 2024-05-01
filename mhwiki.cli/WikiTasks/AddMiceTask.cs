using System.Net.Http.Json;
using System.Text.Json;
using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;

class AddMiceTask : WikiTask
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

        string choice;
        do
        {
            choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("How would you like to check for missing mice?")
                    .PageSize(10)
                    .AddChoices([
                        "1) Create Category pages",
                        "2) Create Mouse Group redirects",
                        "3) Create individual Mice pages",
                        "4) All of these at once!",
                        "5) Go back"
                    ]));

            await (choice[0] switch
            {
                '1' => CreateMissingCategoriesAsync(site, mice),
                '2' => CreateMissingMouseGroupRedirects(site, mice),
                '3' => CreateMissingMicePages(site, mice),
                '4' => Task.CompletedTask,
                _ => Task.CompletedTask
            });
        } while (choice != "Go back");
    }

    private async Task CreateMissingCategoriesAsync(WikiSite site, Mouse[] mice)
    {
        List<string> missingCategories = await GetMissingPagesFor(site, mice, static (m) => $"Category:{m.Group}").ToListAsync();
        missingCategories = missingCategories.Select(s => s.Replace("Category:", "")).ToList();

        missingCategories = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[red]Missing[/] category pages. Select which ones you'd like to [green]create[/].")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a group," +
                    "[green]<enter>[/] to accept)[/]")
                .NotRequired()
                .AddChoices(missingCategories));

        if (missingCategories.Count == 0)
        {
            AnsiConsole.WriteLine("None selected. Returning to previous menu");
            return;
        }

        AnsiConsole.MarkupLine($"You selected: {string.Join(", ", missingCategories.Select(s => $"[green]{s}[/]"))}");
        AnsiConsole.MarkupLine("Here is the page template [blue](with group placeholder text)[/]");
        AnsiConsole.MarkupLine($"""

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
                foreach (var groupName in missingCategories)
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

    const string MouseGroupCategoryTemplate = """
        Listed below are the GROUP_NAME_PLACEHOLDER. Further information on these [[mice]] can be found in the [[Effectiveness]] and [[Mouse Group]] articles.

        [[Category:Mice]]
        """;

    const string MouseGroupRedirectTemplate = """
        #REDIRECT [[Mouse Group#GROUP_NAME_PLACEHOLDER]]
        """;

    const string MousePageTemplate = """
        '''{{PAGENAME}}''' is a breed of mouse found on the in [[Bountiful Beanstalk]].
        {{ Mouse
         | id        = 1140
         | maxpoints = 75,000
         | mingold   = 12,500
         | mgroup    = Beanstalkers
         | subgroup  = Dungeon Dwellers
         | habitat   = [[Bountiful Beanstalk]]
         | loot      = [[Lavish Lapis Bean]]
         | traptype  = [[Physical]]
         | bait      = [[Beanster Cheese]]
         | charm     = None
         | other     = [[Dungeon Floor]]
         | mhinfo    = lethargic_guard
         | image     = {{MHdomain}}/images/mice/large/6c6350b7dc221ebdddfc8ad01bbcf7be.jpg
         | desc      = Guarding all of the inmates of the dungeon is a difficult and tiring task and as this slothful sentry is well aware, the easiest way to deal with a difficult task is... not to do it! The Lethargic Guard is pretty sure that his presence in the dungeon should be enough to keep the prisoners in line and that the "active" part of active duty is largely superfluous.
        }}

        == Cheese Preference ==
        '''{{PAGENAME}}''' is only attracted to [[Beanster Cheese]].

        == Hunting Strategy ==
        The '''{{PAGENAME}}''' can only be attracted after planting a [[Short Vine]] to the [[Dungeon Floor]].

        [[Physical]] power type is effective against '''{{PAGENAME}}'''.<br>
        All other types are ineffective.<br>

        == Loot ==
        '''{{PAGENAME}}''' can drop:

        == History and Trivia ==
        *'''$$RELEASE_DATE$$:''' '''{{PAGENAME}}''' was released as part of $$ENVIRONMENT$$ location.
        
        """;
}
