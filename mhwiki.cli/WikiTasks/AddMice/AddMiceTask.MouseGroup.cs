using System.Net.Http.Json;
using System.Text.Json.Nodes;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    private async Task CreateMissingMouseGroupRedirects(WikiSite site, Mouse[] mice)
    {
        List<string> missingGroups = await GetMissingPagesFor(site, mice, static (m) => m.Group).ToListAsync();

        if (missingGroups.Count == 0 )
        {
            AnsiConsole.WriteLine("No missing mouse group redirect pages.");
            await Task.Delay(2500);

            return;
        }

        missingGroups = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Groups with [red]missing[/] redirect pages. Select which ones you'd like to [green]create[/].")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a group," +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices(missingGroups));

        string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(RedirectTemplate);

        ConsoleKeyInfo? key;
        do
        {
            AnsiConsole.Clear();

            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { To = $"Mouse Group#{missingGroups[0]}" });

            AnsiConsole.MarkupLine($"""
            You selected: {string.Join(", ", missingGroups.Select(s => $"[yellow]{s}[/]"))}

            Here is a [green]preview render[/] of the [blue]Liquid[/] template using the first selection:
            """);

            MarkupPageContent(renderedText);

            AnsiConsole.MarkupLine($"""
            Press (e) to edit [blue]Liquid[/] template, (y) to [green]create pages[/], (n) to [red]cancel[/]
            """);

            key = AnsiConsole.Console.Input.ReadKey(true);
            if (key.Value.Key == ConsoleKey.E)
            {
                await LiquidUtil.EditTemplateWithNotepad(tempFile);
            }
            else if (key.Value.Key == ConsoleKey.N)
            {
                return;
            }
        } while (key!.Value.Key != ConsoleKey.Y);

        AnsiConsole.WriteLine();

        Task<Dictionary<string, string>> getDescriptionsTask = new HttpClient() { BaseAddress = new Uri("https://www.mousehuntgame.com") }
            .GetFromJsonAsync<Environment[]>("/api/get/mousegroup/all", s_serializerOptions)
            .ContinueWith(t =>
            {
                return t.Result!.ToDictionary(e => e.Name, e => e.Description);
            });

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync("[yellow]Creating mouse group page redirect...[/]", async (ctx) =>
            {
                foreach (var groupName in missingGroups)
                {
                    AnsiConsole.MarkupLine(groupName);
                    try
                    {
                        string pageTitle = groupName;
                        string pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);
                        var page = new WikiPage(site, pageTitle);


                        AnsiConsole.MarkupLine($"\tCreating page...");

                        string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { To = $"Mouse Group#{groupName}" });
                        if (Debug)
                        {
                            MarkupPageContent(renderedText);
                        }
                        else
                        {

                            await page.EditAsync(new WikiPageEditOptions()
                            {
                                Content = renderedText,
                                Summary = "Created page using github.com/hymccord/mhwiki-tools"
                            });
                        }

                        AnsiConsole.MarkupLine($"\t{pageUrl.Replace(" ", "_")} [green]Created![/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]Error![/]");
                        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                    }
                }

                ctx.Status("Done!");
            });

        AnsiConsole.MarkupLine("""

            Creating the section in Mouse Group is currently unsupported, but here is the text to copy and a link to the page:
            https://mhwiki.hitgrab.com/wiki/index.php/Mouse_Group

            """);


        var groupToDesc = await getDescriptionsTask;
        foreach (var groupName in missingGroups)
        {
            var obj = new JsonObject();

            JsonArray a =
            [
                ..mice
                    .Where(m => m.Group == groupName)
                    .Select(m => m.Subgroup)
                    .ToHashSet()
            ];

            obj["Group"] = groupName;
            obj["Description"] = groupToDesc[groupName];
            obj["Subgroups"] = a;

            var text = LiquidUtil.Render(MouseGroupSection, obj);
            AnsiConsole.MarkupLine($"""
                [gray]{Markup.Escape(text)}[/]

                """);
        }


        AnsiConsole.WriteLine("Press any key to continue.");
        AnsiConsole.Console.Input.ReadKey(true);
    }

    private record Environment(string Name, string Description);
}
