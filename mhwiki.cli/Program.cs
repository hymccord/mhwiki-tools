using Spectre.Console;

namespace mhwiki.cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var app = new WikiApp();
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
          
    }
}
