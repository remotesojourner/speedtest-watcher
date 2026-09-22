namespace SpeedtestWatcher.Application.Providers;

public sealed record ToolOutput(string Output, string Errors, int ExitCode)
{
    public IEnumerable<string> OutputLines => Lines(Output);

    public IEnumerable<string> ErrorLines => Lines(Errors);

    private static IEnumerable<string> Lines(string text) =>
        text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
}
