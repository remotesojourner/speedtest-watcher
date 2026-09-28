namespace SpeedtestWatcher.Application.Models;

public sealed record ToolArguments(IReadOnlyList<string> Arguments, string? ScratchFileContent = null);
