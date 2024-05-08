using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using mhwiki.cli.Utililty;

using MwParserFromScratch;
using MwParserFromScratch.Nodes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Spectre.Console;

using WikiClientLibrary.Client;
using WikiClientLibrary.Infrastructures;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Parsing;
using WikiClientLibrary.Pages.Queries.Properties;
using WikiClientLibrary.Sites;

namespace mhwiki.cli.WikiTasks;
internal partial class NewsHistoryTask
{
    private async Task UpdatePatchNotes(WikiSite site)
    {
        await LoginAsync();

        

        DateTime lastArchive = GetLastArchiveDate(site);
        // TODO: get last date exactly

        List<ArchivePost> patchNotes = [];

        await AnsiConsole.Status()
            .StartAsync("Looking for unarchived patch notes...", async (ctx) =>
            {
                int pageOffset = 1;
                bool done = false;
                while (!done)
                {
                    ArchivesPage? archivePage = await _apiClient.GetPatchNotes(pageOffset);
                    foreach (ArchivePost post in archivePage?.Posts ?? Enumerable.Empty<ArchivePost>())
                    {
                        // if post is from this month, skip it. We will only archive complete months as of now
                        if (post.PublishDate.Year == DateTime.Now.Year && post.PublishDate.Month == DateTime.Now.Month)
                        {
                            continue;
                        }

                        if (post.PublishDate > lastArchive)
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
        //await page.EditSectionAsync(content.Sections.Count - 1, "== Four ==", new WikiPageEditOptions
        //{
        //    Content = "Three",
        //    Summary = "Adding section",
        //});
        // 2) Get list of mh patch notes since 1)
    }

    private static async Task<int> GetLastArchivedPostId(WikiSite site)
    {
        var parser = new WikitextParser();

        (string year, string month) = await GetLastWikiPatchNoteYearAndMonth(site, parser);

        string postId = await Get

        return 0;

        static async Task<(string year, string month)> GetLastWikiPatchNoteYearAndMonth(WikiSite site, WikitextParser parser)
        {
            const string PatchNotesPageTitle = "Patch Notes";
            // 1) Get date of last wiki patch notes

            var page = new WikiPage(site, PatchNotesPageTitle);
            await page.RefreshAsync(PageQueryOptions.FetchContent);

            var tree = parser.Parse(page.Content!);

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
    }
}
