using WikiClientLibrary;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace mhwiki.cli;

internal class Program
{
    static async void Main(string[] args)
    {
        await WikiStuff();
    }



    private static async Task WikiStuff()
    {
        // A WikiClient has its own CookieContainer.
        var client = new WikiClient
        {
            ClientUserAgent = "WCLQuickStart/1.0 (your user name or contact information here)"
        };
        // You can create multiple WikiSite instances on the same WikiClient to share the state.
        var site = new WikiSite(client, "https://mhwiki.hitgrab.com/wiki/api.php");
        // Wait for initialization to complete.
        // Throws error if any.
        await site.Initialization;
        try
        {
            //await site.LoginAsync();
        }
        catch (WikiClientException ex)
        {
            Console.WriteLine(ex.Message);
            // Add your exception handler for failed login attempt.
        }

        // Do what you want
        Console.WriteLine(site.SiteInfo.SiteName);
        Console.WriteLine(site.AccountInfo);
        Console.WriteLine("{0} extensions", site.Extensions.Count);
        Console.WriteLine("{0} interwikis", site.InterwikiMap.Count);
        Console.WriteLine("{0} namespaces", site.Namespaces.Count);

        // We're done here
        await site.LogoutAsync();
        client.Dispose();        // Or you may use `using` statement.
    }
}
