namespace SpeedtestWatcher.Core.SpeedTest;

public sealed record ToolArguments(IReadOnlyList<string> Arguments, string? ScratchFileContent = null);
