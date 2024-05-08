using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using mhwiki.cli.Utililty;

using MwParserFromScratch;
using MwParserFromScratch.Nodes;

using Pandoc;

using Spectre.Console;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
internal partial class NewsHistoryTask
{
    private async Task UpdatePatchNotes(WikiSite site)
    {
        await LoginAsync();

        //string lastArchivePostId = await PatchNotesHelper.GetLastWikiArchivedPostId(site);

        //if (!int.TryParse(lastArchivePostId, out int postId))
        //{
        //    AnsiConsole.MarkupLine("""
        //        [red]Couldn't get last post id from patch note archive.[/]

        //        Press any key to continue.
        //        """);
        //    AnsiConsole.Console.Input.ReadKey(true);
        //}

        //// Get official MH Archive patch notes
        //var patchNotes = await PatchNotesHelper.GetArchivePatchNotesAsync(_apiClient, postId);

        //PatchNotesHelper.WriteArchivePostTree(patchNotes);

        //AnsiConsole.Clear();

        //var newsPages = await PatchNotesHelper.GetNewsPagesAsync(_apiClient, patchNotes.Select(p => p.NewsPostId));

        //var text = JsonSerializer.Serialize(newsPages, new JsonSerializerOptions
        //{
        //    WriteIndented = true,
        //    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        //});
        //File.WriteAllText("cache.json", text);

        //var cache = File.ReadAllText("cache.json");
        //var newsPages = JsonSerializer.Deserialize<IReadOnlyList<NewsPage>>(cache, JsonSerializerOptionsProvider.RelaxedDateTime);
        //var wikiTexts = await PatchNotesHelper.GenerateWikiText(newsPages!);

        await PatchNotesHelper.GenerateWikiText(new List<NewsPage> { await _apiClient.GetNewsPageByIdAsync("799") });

        // preview
        // create
    }

    private static class PatchNotesHelper
    {
        public static async Task<string> GetLastWikiArchivedPostId(WikiSite site)
        {
            (int year, int month) = await GetMostRecentWikiPatchNoteArchiveYearAndMonth(site);

            string postId = await GetMostRecentWikiPatchNoteArchivePostId(year, month, site);

            return postId;

            static async Task<(int year, int month)> GetMostRecentWikiPatchNoteArchiveYearAndMonth(WikiSite site)
            {
                // 1) Get date of last wiki patch notes
                const string PatchNotesPageTitle = "Patch Notes";
                Wikitext tree = await GetPageWikitextAsync(site, PatchNotesPageTitle);

                int year = 0; int month = 0;
                foreach (LineNode? node in tree.Lines)
                {
                    // ToString will return wikitext
                    if (Regex.Match(node.ToString() ?? string.Empty, @"== (\d+) ==") is Match m && m.Success)
                    {
                        year = int.Parse(m.Groups[1].Value);
                        continue;
                    }

                    if (DateTime.TryParseExact(node.ToPlainText(), "MMMM", null, DateTimeStyles.None, out DateTime date))
                    {
                        month = date.Month;
                        break;
                    }
                }

                return (year, month);
            }

            static async Task<string> GetMostRecentWikiPatchNoteArchivePostId(int year, int month, WikiSite site)
            {
                var datetime = new DateTime(year, month, 1);
                string archivePage = $"Patch Notes/Archive/{datetime:yyyy}/{datetime:MMMM}";

                var tree = await GetPageWikitextAsync(site, archivePage);

                foreach (LineNode? node in tree.Lines)
                {
                    if (Regex.Match(node.ToString() ?? string.Empty, @"={4}\[{{MHdomain}}\/newspost\.php\?news_post_id=(\d+)") is Match m && m.Success)
                    {
                        return m.Groups[1].Value;
                    }
                }
                return string.Empty;
            }

            static async Task<Wikitext> GetPageWikitextAsync(WikiSite site, string title)
            {
                var page = new WikiPage(site, title);
                await page.RefreshAsync(PageQueryOptions.FetchContent);

                var parser = new WikitextParser();
                return parser.Parse(page.Content!);
            }
        }

        public static async Task<IReadOnlyList<ArchivePost>> GetArchivePatchNotesAsync(MouseHuntApiClient client, int postId)
        {
            List<ArchivePost> patchNotes = [];

            await AnsiConsole.Status()
                .StartAsync("Looking for unarchived patch notes...", async (ctx) =>
                {
                    int pageOffset = 1;
                    bool done = false;
                    while (!done)
                    {
                        ArchivesPage? archivePage = await client.ListPatchNotes(pageOffset);
                        foreach (ArchivePost post in archivePage?.Posts ?? Enumerable.Empty<ArchivePost>())
                        {
                            // if post is from this month, skip it. We will only archive complete months as of now
                            if (post.PublishDate.Year == DateTime.Now.Year && post.PublishDate.Month == DateTime.Now.Month)
                            {
                                continue;
                            }

                            if (int.Parse(post.NewsPostId) > postId)
                            {
                                patchNotes.Add(post);
                            }
                            else
                            {
                                done = true;
                                break;
                            }
                        }
                        pageOffset++;
                    }
                });

            return patchNotes;
        }

        public static void WriteArchivePostTree(IReadOnlyList<ArchivePost> posts)
        {
            var root = new Tree("Unarchived Patch Notes");
            foreach (IGrouping<int, ArchivePost> yearGroup in posts.GroupBy(p => p.PublishDate.Year))
            {
                TreeNode yearNode = root.AddNode($"[yellow]{yearGroup.Key}[/]");
                foreach (IGrouping<int, ArchivePost> monthGroup in yearGroup.GroupBy(p => p.PublishDate.Month))
                {
                    var monthLong = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetMonthName(monthGroup.Key);
                    var monthNode = yearNode.AddNode(monthLong);

                    monthNode.AddNodes(monthGroup.Select(p => $"[blue]{p.Title}[/]"));
                }
            }

            AnsiConsole.Write(root);
            AnsiConsole.Console.PromptAnyKeyToContinue();
        }

        public static async Task<IReadOnlyList<NewsPage>> GetNewsPagesAsync(MouseHuntApiClient client, IEnumerable<string> ids)
        {
            List<NewsPage> results = [];

            await AnsiConsole.Progress()
                .Columns([
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                ])
                .StartAsync(async ctx =>
                {
                    var fetchTask = ctx.AddTask("[green]Fetching patch notes content[/]");

                    var downloadQuery = ids.Select(client.GetNewsPageByIdAsync);
                    var downloadTasks = downloadQuery.ToList();

                    double progressPerTask = 100d / downloadTasks.Count;
                    while (downloadTasks.Count != 0)
                    {
                        var finishedTask = await Task.WhenAny(downloadTasks);
                        fetchTask.Increment(progressPerTask);
                        downloadTasks.Remove(finishedTask);

                        results.Add(await finishedTask);
                    }

                    fetchTask.Value = 100;
                });

            return results;
        }

        public static async Task<IReadOnlyList<string>> GenerateWikiText(IReadOnlyList<NewsPage> pages)
        {
            var engine = new PandocEngine();
            var instance = await engine.ConvertToText<HtmlIn, MediaWikiOut>(pages[0].Body);

            throw new NotImplementedException();
        }
    }

}
