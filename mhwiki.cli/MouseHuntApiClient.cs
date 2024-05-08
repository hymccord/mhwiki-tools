using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using mhwiki.cli.Utililty;

namespace mhwiki.cli;
internal class MouseHuntApiClient
{
    private const string HgSessionFile = "mousehunt_session.json";
    private static readonly Uri s_mouseHuntBaseAddress = new("https://www.mousehuntgame.com");
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private readonly IList<KeyValuePair<string, string>> _defaultFormData = [
        new ("sn", "Hitgrab"),
        new ("hg_is_ajax", "1")
    ];

    public MouseHuntApiClient()
    {
        _cookieContainer = new CookieContainer();
        var handler = new SocketsHttpHandler()
        {
            CookieContainer = _cookieContainer,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = s_mouseHuntBaseAddress,
        };

        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("mhwiki-tools", "1.0"));
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
    }

    public async Task<bool> InitializeAsync()
    {
        await LoadSessionAsync();
        bool loginRequired = await IsAuthenticationRequiredAsync();

        return !loginRequired;
    }

    public async Task<ArchivesPage?> GetPatchNotes(int page = 1)
    {
        var response = await SendRequestAsync("/managers/ajax/pages/news_archives.php",
            [
                new ("category", "ptc"),
                new ("page", $"{page}"),
            ]);

        return response.RootElement.GetProperty("archives_page")
            .Deserialize<ArchivesPage>(JsonSerializerOptionsProvider.RelaxedDateTime);
    }

    private async Task<JsonDocument> SendRequestAsync(string relativeUri, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var content = new FormUrlEncodedContent([
            .._defaultFormData,
            ..parameters,
        ]);

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(relativeUri, UriKind.Relative),
            Method = HttpMethod.Post,
            Content = content
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
        {
            CharSet = "UTF-8"
        };

        HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    #region Session Management

    public async Task LoginAsync(string username, string password)
    {
        // This will get a HG_TOKEN in the cookie container if the load session didn't have one or is expired (I think)
        // Login
        JsonDocument response = await SendRequestAsync("/managers/ajax/users/session.php", [
                new ("action", "loginHitGrab"),
                    new("username", username),
                    new("password", password)
            ]);
        HgResponse? hgResponse = response.Deserialize<HgResponse>(JsonSerializerOptionsProvider.Default);

        // Save session
        if (hgResponse is not null)
        {
            await SaveSessionAsync(hgResponse.User.UniqueHash);
        }

        async Task SaveSessionAsync(string uniqueHash)
        {
            Cookie? token = _cookieContainer.GetCookies(s_mouseHuntBaseAddress)["HG_TOKEN"];
            if (token is null)
            {
                return;
            }

            var session = new HgSession
            {
                HgToken = new HgSession.SavedCookie(token.Name, token.Value, token.Path, token.Domain, token.Expires),
                UniqueHash = uniqueHash
            };

            await File.WriteAllTextAsync(HgSessionFile, JsonSerializer.Serialize(session));
        }
    }

    async Task<bool> IsAuthenticationRequiredAsync()
    {
        try
        {
            await SendRequestAsync("/managers/ajax/pages/page.php", [
                new ("page_class", "News"),
                ]);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.InternalServerError)
        {
            return true;
        }

        return false;
    }

    private async Task LoadSessionAsync()
    {
        if (!File.Exists(HgSessionFile))
        {
            return;
        }

        using FileStream file = File.OpenRead(HgSessionFile);
        HgSession? hg = await JsonSerializer.DeserializeAsync<HgSession>(file);

        if (hg is null)
        {
            return;
        }

        _defaultFormData.Add(new("uh", hg.UniqueHash));
        _cookieContainer.Add(new Cookie(hg.HgToken.Name, hg.HgToken.Value, hg.HgToken.Path, hg.HgToken.Domain)
        {
            Expires = hg.HgToken.Expires,
        });
    }

    #endregion


    public class HgSession
    {
        public required SavedCookie HgToken { get; init; }
        public required string UniqueHash { get; set; }

        internal record SavedCookie(string Name, string Value, string Path, string Domain, DateTime Expires);
    }

}

public class DateTimeConverterUsingDateTimeParse : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTime));
        return DateTime.Parse(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class HgResponse
{
    public required HgUser User { get; set; }
}

public class HgUser
{
    public required string UniqueHash { get; set; }
}

public class ArchivesPage
{
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required IReadOnlyCollection<ArchivePost> Posts { get; init; }
}

public class ArchivePost
{
    public required string NewsPostId { get; init; }
    public required string Title { get; init; }
    public required PostAuthor Author { get; init; }
    public DateTime PublishDate { get; init; }
}

public class PostAuthor
{
    public string Name { get; set; } = string.Empty;
    public string SnUserId { get; set; } = string.Empty;
}
