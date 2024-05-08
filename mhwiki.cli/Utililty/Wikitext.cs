using System.Text.RegularExpressions;

namespace mhwiki.cli.Utililty;
internal class Wikitext
{
    /// <summary>
    /// A rather simple method (but working in most cases) to parse all the section from the wikitext.
    /// </summary>
    public static IReadOnlyList<ParsedSectionInfo> WikitextParseSections(string content)
    {
        var matches = Regex.Matches(content, @"^(?'LEFT'={1,8})(?'TITLE'[^=].*?)\k<LEFT>\s*$",
            RegexOptions.Multiline);
        var parsedSections = new List<ParsedSectionInfo>();
        string? lastSectionTitle = null;
        var lastSectionStartsAt = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var sectionStartsAt = match.Index;
            parsedSections.Add(new ParsedSectionInfo(i, lastSectionTitle, content[lastSectionStartsAt..sectionStartsAt]));
            lastSectionTitle = match.Groups["TITLE"].Value.Trim();
            lastSectionStartsAt = sectionStartsAt;
        }
        parsedSections.Add(new ParsedSectionInfo(matches.Count, lastSectionTitle, content[lastSectionStartsAt..]));
        return parsedSections;
    }

    /// <param name="Title">Section title.</param>
    /// <param name="Content">Section content, including the title part.</param>
    public record ParsedSectionInfo(int id, string? Title, string Content);
}
