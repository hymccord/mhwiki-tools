using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    private async Task CreateMissingMicePages(WikiSite site, Mouse[] mice)
    {
        var miceDict = mice.ToDictionary(m => m.Name);
        List<string> missingMiceNames = [];

        await AnsiConsole.Status()
            .StartAsync("Looking for missing mice pages...", async (ctx) =>
        {
            missingMiceNames = await GetMissingPagesFor(site, mice, static (m) => m.Name).ToListAsync();
        });

        if (missingMiceNames.Count == 0)
        {
            AnsiConsole.WriteLine("No missing pages. Returning...");
            await Task.Delay(2500);
            return;
        }

        var groupingsByMouseGroup = missingMiceNames.Select(m => miceDict[m]).GroupBy(m => m.Group);

        MultiSelectionPrompt<string> prompt = new MultiSelectionPrompt<string>()
            .Title("[red]Missing[/] mice pages. Select which ones you'd like to [green]create[/].")
            .PageSize(20)
            .MoreChoicesText("[grey](Move up and down to reveal more groups)[/]")
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a group," +
                "[green]<enter>[/] to accept)[/]")
            .NotRequired();

        foreach (IGrouping<string, Mouse> mouseGroupGrouping in groupingsByMouseGroup)
        {
            IMultiSelectionItem<string> root = prompt.AddChoice(mouseGroupGrouping.Key);
            foreach (IGrouping<string, Mouse> subGroupGrouping in mouseGroupGrouping.GroupBy(m => m.Subgroup))
            {
                ISelectionItem<string> sub = root.AddChild(subGroupGrouping.Key);
                foreach (Mouse mouse in subGroupGrouping)
                {
                    sub.AddChild(mouse.Name);
                }
            }
        }

        missingMiceNames = AnsiConsole.Prompt(prompt);

        IEnumerable<Mouse> missingMice = missingMiceNames.Select(name => miceDict[name]);

        var miceByGroup = missingMice.GroupBy(m => m.Group).ToDictionary(g => g.Key, g => g.ToList());
        var miceBySubgroup = missingMice.GroupBy(m => m.Subgroup).ToDictionary(g => g.Key, g => g.ToList());
        var miceByName = missingMice.ToDictionary(m => m.Name, m => new List<Mouse>() { m });

        var howToGroup = AnsiConsole.Prompt(
            new SelectionPrompt<DisplayFunc<Dictionary<string, List<Mouse>>>>()
                .Title("How would you like to edit the [blue]liquid[/] templates?")
                .AddChoices([
                    new DisplayFunc<Dictionary<string, List<Mouse>>>("By group", () => Task.FromResult(miceByGroup!)),
                    new DisplayFunc<Dictionary<string, List<Mouse>>>("By subgroup [green](generally best)[/]", () => Task.FromResult(miceBySubgroup!)),
                    new DisplayFunc<Dictionary<string, List<Mouse>>>("By mouse", () => Task.FromResult(miceByName)),
                    ]));

        var toEdit = await howToGroup.Invoke();

        ConsoleKeyInfo? key;

        string globalPropsFile = await LiquidUtil.CreateFileFromTemplateAsync(GlobalPropsTemplate);
        if (AnsiConsole.Confirm("Edit global data to be available in the templates? (e.g. Release Date, Location)"))
        {
            await LaunchJsonEditorAsync(globalPropsFile);
        }

        var editedTemplateByKey = new Dictionary<string, string>();
        foreach ((string groupKey, List<Mouse> mouseList) in toEdit)
        {
            string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(MousePageTemplate);
            do
            {
                AnsiConsole.Clear();

                JsonObject model = MouseToModel(mouseList[0], globalPropsFile);
                string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, model);

                AnsiConsole.MarkupLine($"""
                You are editing templates: [yellow]{groupKey}[/]

                Here is a [green]preview render[/] of the [blue]Liquid[/] template using the first selection:
                """);

                MarkupPageContent(renderedText);

                AnsiConsole.MarkupLine($"""
                Press (e) to edit [blue]Liquid[/] template, (d) to edit global data, (y) to [green]create pages[/], (n) to [red]cancel[/]
                """);

                key = AnsiConsole.Console.Input.ReadKey(true);
                if (key.Value.Key == ConsoleKey.E)
                {
                    await LiquidUtil.EditTemplateWithNotepad(tempFile);
                }
                else if (key.Value.Key == ConsoleKey.D)
                {
                    await LaunchJsonEditorAsync(globalPropsFile);
                }
                else if (key.Value.Key == ConsoleKey.N)
                {
                    return;
                }
            } while (key!.Value.Key != ConsoleKey.Y);

            editedTemplateByKey[groupKey] = tempFile;
        }

        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync("[yellow]Creating mouse pages...[/]", async (ctx) =>
            {
                foreach ((string groupKey, List<Mouse> mouseList) in toEdit)
                {
                    foreach (Mouse mouse in mouseList)
                    {
                        AnsiConsole.MarkupLine($"{mouse.Name}");
                        try
                        {
                            string pageTitle = mouse.Name;
                            string pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);
                            var page = new WikiPage(site, pageTitle);

                            AnsiConsole.MarkupLine("\tCreating mouse page.");

                            string renderedText = await LiquidUtil.RenderTemplateFromFile(editedTemplateByKey[groupKey], MouseToModel(mouse, globalPropsFile));
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

                            pageTitle = mouse.AbbreviatedName;
                            page = new WikiPage(site, pageTitle);
                            pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);

                            if (mouse.Name != mouse.AbbreviatedName)
                            {

                                AnsiConsole.MarkupLine("\tCreating abbreviated name redirect page.");
                                renderedText = LiquidUtil.Render(RedirectTemplate, new { To = mouse.Name });
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

                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine("[red]Error![/]");
                            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                        }
                    }
                }

                ctx.Status("Done!");
            });

        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine("Press any key to continue.");
        AnsiConsole.Console.Input.ReadKey(true);

        static JsonObject MouseToModel(Mouse mouse, string globalPropsFile)
        {
            JsonArray weaknesses = [];
            if (mouse.Weaknesses.TryGetValue(Effectiveness.VeryEffective, out var veryEff))
            {
                weaknesses = [..veryEff.Select(p => p.ToString())];
            }
            else if (mouse.Weaknesses.TryGetValue(Effectiveness.Effective, out var eff))
            {
                weaknesses = [.. eff.Select(p => p.ToString())];
            }
            else if (mouse.Weaknesses.TryGetValue(Effectiveness.LessEffective, out var lessEff))
            {
                weaknesses = [.. lessEff.Select(p => p.ToString())];
            }

            var o = new JsonObject();
            o["Id"] = mouse.Id;
            o["Type"] = mouse.Type;
            o["Name"] = mouse.Name;
            o["AbbreviatedName"] = mouse.AbbreviatedName;
            o["Description"] = mouse.Description.Replace("\t", "").Replace("\n", "").Replace("<br />", "\n");
            o["Points"] = mouse.Points;
            o["PointsFormatted"] = mouse.PointsFormatted;
            o["GoldFormatted"] = mouse.GoldFormatted;
            o["Group"] = mouse.Group;
            o["Subgroup"] = mouse.Subgroup;
            o["Image"] = mouse.Images.Large.AbsolutePath;
            o["Weaknesses"] = weaknesses;

            JsonObject global = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(globalPropsFile), s_serializerOptions)!;

            foreach ((string property, JsonNode? node) in global)
            {
                o.Add(property, node?.DeepClone());
            }

            return o;
        }
    }

    private async Task LaunchJsonEditorAsync(string filePath)
    {
        while (true)
        {
            var process = Process.Start("notepad.exe", filePath);
            await process.WaitForExitAsync();

            try
            {
                JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(filePath), s_serializerOptions);
                return;
            }
            catch
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[red]Invalid json.[/] Try again.");
            }
        }
    }
}
