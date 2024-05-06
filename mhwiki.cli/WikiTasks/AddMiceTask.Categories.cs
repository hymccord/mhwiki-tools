using System.Diagnostics;

using Fluid;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    private async Task CreateMissingCategoriesAsync(WikiSite site, Mouse[] mice)
    {
        //List<string> missingGroups = [];
        //List<string> missingSubgroups = [];
        //await AnsiConsole.Status()
        //    .StartAsync("Finding missing category pages...", async (ctx) =>
        //    {
        //        missingGroups = await GetMissingPagesFor(site, mice, static (m) => $"Category:{m.Group}").ToListAsync();
        //        missingSubgroups = await GetMissingPagesFor(site, mice.Where(m => !string.IsNullOrEmpty(m.Subgroup)), static (m) => $"Category:{m.Group} ({m.Subgroup})").ToListAsync();
        //    });

        //List<DisplayFunc> tasks = [];
        //if (missingGroups.Count > 0)
        //{
        //    tasks.Add(new DisplayFunc("Add missing mouse group category pages", async () => CreateMissingGroupCategoriesAsync(site, missingGroups));
        //}

        //if (missingSubgroups.Count > 0)
        //{
        //    tasks.Add(new DisplayFunc("Add missing mouse subgroup category pages", async () => CreateMissingGroupCategoriesAsync(site, missingSubgroups));
        //}

        //if (tasks.Count == 0)
        //{
        //    AnsiConsole.MarkupLine("There are no missing category pages!");
        //    await Task.Delay(2500);
        //    return;
        //}

        //var choice = AnsiConsole.Prompt(
        //     new MultiSelectionPrompt<DisplayFunc>()
        //         .Title("S")
        //         .PageSize(10)
        //         .DefaultInstructionsText()
        //         .AddChoices([
        //             new DisplayFunc("Create category pages", async () => await CreateMissingCategoriesAsync(site, mice)),
        //             new DisplayFunc("Create mouse group redirects", async () => await CreateMissingMouseGroupRedirects(site, mice)),
        //             new DisplayFunc("Create individual mouse pages", async () => await CreateMissingMicePages(site, mice)),
        //             new DisplayFunc("Update general mouse page", () => Task.CompletedTask),
        //             new DisplayFunc("Go back", () => Task.CompletedTask)
        //            ]));

        //foreach (var task in choice)
        //{
        //    await task.Invoke();
        //}

        await CreateMissingGroupCategoriesAsync(site, mice);
    }

    private async Task CreateMissingGroupCategoriesAsync(WikiSite site, Mouse[] mice)
    {
        List<string> missingCategories = await GetMissingPagesFor(site, mice, static (m) => $"Category:{m.Group}").ToListAsync();
        missingCategories = missingCategories.Select(s => s.Replace("Category:", "")).ToList();

        missingCategories = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[red]Missing[/] group category pages. Select which ones you'd like to [green]create[/].")
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

        string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(MouseGroupCategoryTemplate);

        ConsoleKeyInfo? key;
        do
        {
            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = missingCategories[0] });

            AnsiConsole.MarkupLine($"""
            You selected: {string.Join(", ", missingCategories.Select(s => $"[yellow]{s}[/]"))}

            Here is a [green]preview render[/] of the [blue]Liquid[/] template using the first selection:
            """);

            AnsiConsole.Write(new Panel($"[grey]{renderedText}[/]"));

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
            .StartAsync("[yellow]Creating category pages[/]", async (ctx) =>
            {
                foreach (var groupName in missingCategories)
                {
                    try
                    {
                        string pageTitle = $"Category:{groupName}";
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

    private async Task CreateMissingSubcategoriesAsync(WikiSite site, Mouse[] mice)
    {
        List<string> missingCategories = await GetMissingPagesFor(site, mice.Where(m => !string.IsNullOrEmpty(m.Subgroup)), static (m) => $"Category:{m.Group} ({m.Subgroup}").ToListAsync();
        missingCategories = missingCategories.Select(s => s.Replace("Category:", "")).ToList();

        missingCategories = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[red]Missing[/] subgroup category pages. Select which ones you'd like to [green]create[/].")
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

        string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(MouseGroupCategoryTemplate);

        ConsoleKeyInfo? key;
        do
        {
            string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, new { MouseGroup = missingCategories[0] });

            AnsiConsole.MarkupLine($"""
            You selected: {string.Join(", ", missingCategories.Select(s => $"[yellow]{s}[/]"))}

            Here is a [green]preview render[/] of the [blue]Liquid[/] template using the first selection:
            """);

            AnsiConsole.Write(new Panel($"[grey]{renderedText}[/]"));

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
            .StartAsync("[yellow]Creating category pages[/]", async (ctx) =>
            {
                foreach (var groupName in missingCategories)
                {
                    try
                    {
                        string pageTitle = $"Category:{groupName}";
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
