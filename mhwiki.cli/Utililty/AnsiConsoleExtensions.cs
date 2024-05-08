using Spectre.Console;

namespace Spectre.Console;

public static class AnsiConsoleExtensions
{
    public static void PromptAnyKeyToContinue(this IAnsiConsole console)
    {
        console.MarkupLine("Press any key to continue.");
        console.Input.ReadKey(true);
    }
}
