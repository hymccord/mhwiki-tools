using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    private async Task CreateMissingMouseGroupRedirects(WikiSite site, Mouse[] mice)
    {
        List<string> missingGroups = await GetMissingPagesFor(site, mice, static (m) => m.Group).ToListAsync();

        missingGroups = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Mouse groups with [red]missing[/] group pages. Select which ones you'd like to [green]create[/].")
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
            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { To = $"Mouse Group#{missingGroups[0]}" });

            AnsiConsole.MarkupLine($"""
            You selected: {string.Join(", ", missingGroups.Select(s => $"[yellow]{s}[/]"))}

            Here is a [green]preview render[/] of the [blue]Liquid[/] template using the first selection:
            """);

            var p = new Panel($"[grey]{renderedText}[/]")
            {
                Expand = true,
                Header = new PanelHeader("Page Content", Justify.Center)
            };
            AnsiConsole.Write(p);

            AnsiConsole.MarkupLine($"""
            Press (e) to edit [blue]Liquid[/] template, (y) to continue, (n) to cancel
            """);

            key = AnsiConsole.Console.Input.ReadKey(true);
            if (key.Value.Key == ConsoleKey.E)
            {
                await LiquidUtil.EditTemplateWithNotepad(tempFile);
                AnsiConsole.Clear();
            }
            else if (key.Value.Key == ConsoleKey.N)
            {
                return;
            }
        } while (key!.Value.Key != ConsoleKey.Y);

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync("[yellow]Creating mouse group pages[/]", async (ctx) =>
            {
                foreach (var groupName in missingGroups)
                {
                    try
                    {
                        string pageTitle = groupName;
                        string pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);
                        var page = new WikiPage(site, pageTitle);


                        ctx.Status($"Creating {pageUrl}");

                        if (false)
                        {
                            await Task.Delay(3000);
                        }
                        else
                        {
                            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = groupName });

                            await page.EditAsync(new WikiPageEditOptions()
                            {
                                Content = renderedText,
                                Summary = "Created page using github.com/hymccord/mhwiki-tools"
                            });
                        }

                        AnsiConsole.MarkupLine($"{pageUrl} [green]Created![/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]Error![/]");
                        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                    }
                }

                ctx.Status("Done!");
            });

        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine("Press any key to continue.");
        AnsiConsole.Console.Input.ReadKey(true);
    }
}
