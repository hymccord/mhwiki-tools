namespace mhwiki.cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var app = new WikiApp();
        await app.RunAsync();
    }
}
