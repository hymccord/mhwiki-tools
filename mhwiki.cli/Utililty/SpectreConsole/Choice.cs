namespace mhwiki.cli.Utililty.SpectreConsole;
class Choice(string name, Func<Task> func)
{
    public override string ToString() => name;

    public async Task Invoke() => await func();
}

class Choice<T>(string name, Func<Task<T>> func)
{
    public override string ToString() => name;

    public async Task<T> Invoke() => await func();
}
