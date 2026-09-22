namespace SpeedtestWatcher.Application.Providers;

public sealed record ToolArguments(IReadOnlyList<string> Arguments, string? ScratchFileContent = null);
