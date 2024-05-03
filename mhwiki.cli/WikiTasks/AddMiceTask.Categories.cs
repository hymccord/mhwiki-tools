﻿using System.Diagnostics;

using Fluid;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
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

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, MouseGroupCategoryTemplate);

        ConsoleKeyInfo? key;
        var templateFilePath = await File.ReadAllTextAsync(tempFile);
        do
        {
            AnsiConsole.MarkupLine($"""
            Here is the current [blue]Liquid[/] template

            [grey]{templateFilePath}[/]

            (e) to edit template and data, (r) to render preview, (y) to continue, (n) to cancel
            """);

            key = AnsiConsole.Console.Input.ReadKey(true);
            if (key.Value.Key == ConsoleKey.E)
            {
                var process = Process.Start("notepad.exe", tempFile);
                await process.WaitForExitAsync();
                AnsiConsole.Clear();
            }
            else if (key.Value.Key == ConsoleKey.R)
            {
                AnsiConsole.Clear();
                var parser = new FluidParser();
                if (parser.TryParse(templateFilePath, out var template))
                {
                    var context = new TemplateContext(new { MouseGroup = missingCategories[0] });

                    AnsiConsole.MarkupLine($"""
                        [yellow]Rendered[/]:

                        [grey]{template.Render(context)}[/]

                        """);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Error in parsing Liquid template.[/]");
                }
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
                        var pageTitle = $"Category:{groupName}";
                        var pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);
                        var page = new WikiPage(site, pageTitle);


                        ctx.Status($"Creating {pageUrl}...");
                        await Task.Delay(3000);
                        AnsiConsole.MarkupLine($"Creating {pageUrl}... [green]Done![/]");

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
        AnsiConsole.WriteLine("Press any key to continue.");
        AnsiConsole.Console.Input.ReadKey(false);
    }
}