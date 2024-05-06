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

        Dictionary<string, string> globalProps = [];
        if (AnsiConsole.Confirm("Would you like to make any global properties to be available in the templates? (e.g. Release Date, Location)"))
        {
            int howMany = AnsiConsole.Ask<int>("How many?");
            for (int i = 0; i < howMany; i++)
            {
                string name = AnsiConsole.Ask<string>($"{i}) Name: ");
                string value = AnsiConsole.Ask<string>($"{i}) Value: ");
                globalProps[name] = value;
            }
        }

        var editedTemplateByKey = new Dictionary<string, string>();
        foreach ((string groupKey, List<Mouse> mouseList) in toEdit)
        {
            string tempFile = await LiquidUtil.CreateFileFromTemplateAsync(MousePageTemplate);
            do
            {
                AnsiConsole.Clear();

                JsonObject model = MouseToModel(mouseList[0], globalProps.Select(kvp => (kvp.Key, kvp.Value)).ToArray());
                string renderedText = await LiquidUtil.RenderTemplateFromFile(tempFile, model);

                AnsiConsole.MarkupLine($"""
                You are editing templates: [yellow]{groupKey}[/]

                Here is a [green]preview render[/] of the [blue]Liquid[/] template using the first selection:
                """);

                var p = new Panel($"[grey]{Markup.Escape(renderedText)}[/]")
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
                            if (Debug)
                            {
                                await Task.Delay(1500);
                            }
                            else
                            {
                                string renderedText = await LiquidUtil.RenderTemplateFromFile(editedTemplateByKey[groupKey], MouseToModel(mouseList[0], globalProps.Select(kvp => (kvp.Key, kvp.Value)).ToArray()));

                                await page.EditAsync(new WikiPageEditOptions()
                                {
                                    Content = renderedText,
                                    Summary = "Created page from template"
                                });
                            }

                            AnsiConsole.MarkupLine($"\t{pageUrl.Replace(" ", "_")} [green]Created![/]");

                            pageTitle = mouse.AbbreviatedName;
                            page = new WikiPage(site, pageTitle);
                            pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", pageTitle);

                            if (mouse.Name != mouse.AbbreviatedName)
                            {

                                AnsiConsole.MarkupLine("\tCreating abbreviated name redirect page.");
                                if (Debug)
                                {
                                    await Task.Delay(1500);
                                }
                                else
                                {
                                    string renderedText = await LiquidUtil.RenderTemplateFromFile(RedirectTemplate, new { To = mouse.Name });

                                    await page.EditAsync(new WikiPageEditOptions()
                                    {
                                        Content = renderedText,
                                        Summary = "Created page from template"
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

        static JsonObject MouseToModel(Mouse mouse, params (string, string)[] extraProps)
        {
            var weaknesses = "";
            if (mouse.Weaknesses.TryGetValue(Effectiveness.VeryEffective, out var veryEff))
            {
                weaknesses = string.Join("\n", veryEff);
            }
            else if (mouse.Weaknesses.TryGetValue(Effectiveness.Effective, out var eff))
            {

                weaknesses = string.Join("\n", eff);
            }
            else if (mouse.Weaknesses.TryGetValue(Effectiveness.LessEffective, out var lessEff))
            {
                weaknesses = string.Join("\n", lessEff);
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

            foreach (var prop in extraProps)
            {
                o[prop.Item1] = prop.Item2;
            }

            return o;
        }
    }
}
