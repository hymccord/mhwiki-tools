using System.Text.RegularExpressions;

using mhwiki.cli.Utililty.SpectreConsole;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    private async Task CreateMissingCategoriesAsync(WikiSite site, Mouse[] mice)
    {
        List<string> missingGroups = [];
        List<string> missingSubgroups = [];
        await AnsiConsole.Status()
            .StartAsync("Finding missing category pages...", async (ctx) =>
            {
                missingGroups = await GetMissingPagesFor(site, mice, static (m) => $"Category:{m.Group}").ToListAsync();
                missingSubgroups = await GetMissingPagesFor(site, mice.Where(m => !string.IsNullOrEmpty(m.Subgroup)), static (m) => $"Category:{m.Group} ({m.Subgroup})").ToListAsync();
            });

        List<Choice> tasks = [];
        if (missingGroups.Count > 0)
        {
            tasks.Add(new Choice("Add missing mouse group category pages", async () => await CreateMissingGroupCategoriesAsync(site, mice, missingGroups)));
        }

        if (missingSubgroups.Count > 0)
        {
            tasks.Add(new Choice("Add missing mouse subgroup category pages", async () => await CreateMissingSubcategoriesAsync(site, mice, missingSubgroups)));
        }

        if (tasks.Count == 0)
        {
            AnsiConsole.MarkupLine("There are no missing category pages!");
            await Task.Delay(2500);
            return;
        }

        var choice = AnsiConsole.Prompt(
             new MultiSelectionPrompt<Choice>()
                 .Title("Found missing pages. Select tasks:")
                 .PageSize(10)
                 .DefaultInstructionsText()
                 .AddChoices(tasks));

        foreach (Choice? task in choice)
        {
            await task.Invoke();
        }
    }

    private async Task CreateMissingGroupCategoriesAsync(WikiSite site, Mouse[] mice, IEnumerable<string> missingGroupCategories)
    {
        // Convert page title "Category:<MouseGroup>" into just "MouseGroup"
        List<string> missingGroups = missingGroupCategories
            .Select(t => t.Replace("Category:", ""))
            .ToList();

        missingGroups = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[red]Missing[/] group category pages. Select which ones you'd like to [green]create[/].")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a group," +
                    "[green]<enter>[/] to accept)[/]")
                .NotRequired()
                .AddChoices(missingGroups));

        if (missingGroups.Count == 0)
        {
            AnsiConsole.WriteLine("None selected. Returning to previous menu");
            return;
        }

        string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(MouseGroupCategoryTemplate);

        ConsoleKeyInfo? key;
        do
        {
            AnsiConsole.Clear();

            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = missingGroups[0] });

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

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync("[yellow]Creating category pages...[/]", async (ctx) =>
            {
                foreach (var groupName in missingGroups)
                {
                    AnsiConsole.MarkupLine($"Category:{groupName}");
                    try
                    {
                        string pageTitle = $"Category:{groupName}";
                        string pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);
                        var page = new WikiPage(site, pageTitle);

                        AnsiConsole.MarkupLine($"\tCreating page...");

                        string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = groupName });
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

        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine("Press any key to continue.");
        AnsiConsole.Console.Input.ReadKey(true);
    }

    private async Task CreateMissingSubcategoriesAsync(WikiSite site, Mouse[] mice, IEnumerable<string> missingSubgroupCategories)
    {
        // Convert page title "Category:<MouseGroup> (<Subgroup>)" into just "<MouseGroup> (<SubGroup>)"
        List<string> missingSubgroups = missingSubgroupCategories
            .Select(t => t.Replace("Category:", ""))
            .ToList();

        missingSubgroups = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[red]Missing[/] subgroup category pages. Select which ones you'd like to [green]create[/].")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a group," +
                    "[green]<enter>[/] to accept)[/]")
                .NotRequired()
                .AddChoices(missingSubgroups));

        if (missingSubgroups.Count == 0)
        {
            AnsiConsole.WriteLine("None selected. Returning to previous menu");
            return;
        }

        string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(MouseSubgroupCategoryTemplate);

        ConsoleKeyInfo? key;
        do
        {
            AnsiConsole.Clear();

            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = GetGroupAndSubgroup(missingSubgroups[0]).group });

            AnsiConsole.MarkupLine($"""
            You selected: {string.Join(", ", missingSubgroups.Select(s => $"[yellow]{s}[/]"))}

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

        // Line for spacing
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync("[yellow]Creating subgroup category pages...[/]", async (ctx) =>
            {
                foreach (var subgroupPageTitle in missingSubgroups)
                {
                    AnsiConsole.MarkupLine($"{subgroupPageTitle}");

                    (string groupName, string subGroup) = GetGroupAndSubgroup(subgroupPageTitle);
                    try
                    {
                        string pageTitle = $"Category:{subgroupPageTitle}";
                        string pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);
                        var page = new WikiPage(site, pageTitle);

                        AnsiConsole.MarkupLine($"\tCreating page...");

                        string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = groupName});
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

        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine("Press any key to continue.");
        AnsiConsole.Console.Input.ReadKey(true);

        (string group, string subGroup) GetGroupAndSubgroup(string input)
        {
            var m = Regex.Match(input, @"(?<group>.*)(?= \() \((?<subGroup>.*)\)");

            return (m.Groups["group"].Value, m.Groups["subGroup"].Value);
        }
    }
}
