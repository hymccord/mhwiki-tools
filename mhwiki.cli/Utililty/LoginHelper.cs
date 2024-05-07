using Spectre.Console;

namespace mhwiki.cli.Utililty;
internal static class LoginHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="login"></param>
    /// <param name="maxTries"></param>
    /// <returns></returns>
    public static async Task<bool> StartWithRetries(string identity, Func<string, string, Task> login, int maxTries = 3)
    {
        bool authenticated = false;
        int tries = 0;
        while (!authenticated)
        {
            if (tries >= maxTries)
            {
                AnsiConsole.MarkupLine("""
                    [red]Login failed.

                    Too many attempts![/]
                    """);

                break;
            }

            if (tries == 0)
            {
                AnsiConsole.MarkupLine($"{identity} credentials are either [yellow]expired[/] or [red]missing[/].");
            }

            if (tries > 0)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"Login failed. Try again. ([red]{tries}/{maxTries}[/])");
            }

            authenticated = await LoginWithPromptAsync();
            tries++;
        }

        async Task<bool> LoginWithPromptAsync()
        {
            string username = AnsiConsole.Prompt(
                new TextPrompt<string>($"Enter {identity} [green]username[/]:")
                    .PromptStyle("yellow"));
            string password = AnsiConsole.Prompt(
                new TextPrompt<string>($"Enter {identity} [green]password[/]:")
                    .PromptStyle("red")
                    .Secret('*'));

            bool success = await AnsiConsole.Status()
                .StartAsync("[yellow]Logging in...[/]", async (ctx) =>
                {
                    try
                    {
                        await login(username, password);
                        return true;
                    }
                    catch { }
                    return false;
                });

            return success;
        }

        return authenticated;
    }
}
