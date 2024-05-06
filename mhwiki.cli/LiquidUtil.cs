using System.Diagnostics;

using Fluid;

using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace mhwiki.cli;
internal static class LiquidUtil
{
    public static async Task<string> CreateFileFromTemplateAsync(string template)
    {
        string tempFile = Path.GetTempFileName();

        await File.WriteAllTextAsync(tempFile, template);

        return tempFile;
    }

    public static async Task EditTemplateWithNotepad(string filePath)
    {
        var process = Process.Start("notepad.exe", filePath);
        await process.WaitForExitAsync();
    }

    public static async Task<string> RenderTemplateFromFile(string filePath, object model)
    {
        string text = await File.ReadAllTextAsync(filePath);

        if (new FluidParser().TryParse(text, out IFluidTemplate? template))
        {
            return template.Render(new TemplateContext(model));
        }

        return "[red]Error in parsing Liquid template.[/]";
    }
}

internal static class WikiUtil
{
    public static async Task CreatePageAsync(WikiSite site, string title, string content)
    {
        string pageUrl = site.SiteInfo.ServerUrl + site.SiteInfo.ArticlePath.Replace("$1", title);
        var page = new WikiPage(site, title);

        await page.EditAsync(new WikiPageEditOptions()
        {
            Content = content,
            Summary = "Created page using github.com/hymccord/mhwiki-tools"
        });
    }
}
