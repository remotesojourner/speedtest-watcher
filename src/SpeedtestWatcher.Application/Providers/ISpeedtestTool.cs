namespace SpeedtestWatcher.Application.Providers;

public interface ISpeedtestTool
{
    SpeedtestProvider Provider { get; }

    string Title { get; }

    string Description { get; }

    string BinaryName { get; }

    ServerCatalog? Servers { get; }

    string? DownloadUrl(PlatformTarget target);

    ToolArguments BuildArguments(RunOptions options);

    SpeedtestExecutionResult? ParseResult(string output);
}
