namespace SpeedtestWatcher.Application.Providers;

internal static class ToolFailure
{
    public static SpeedtestExecutionResult Because(string message) => new() { Success = false, Error = message };

    public static SpeedtestExecutionResult Unrecognised(string title, ToolOutput output)
    {
        var lines = output.ErrorLines.Concat(output.OutputLines).ToList();
        if (lines.Any(line => line.Contains("Too many requests", StringComparison.OrdinalIgnoreCase)))
            return Because($"{title} is limiting how often tests can run. Try again later.");

        return Because(lines.FirstOrDefault() is { } first
            ? $"{title} stopped with an error: {first}"
            : $"{title} stopped without a result (exit code {output.ExitCode}).");
    }
}
