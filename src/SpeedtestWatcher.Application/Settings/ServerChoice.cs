namespace SpeedtestWatcher.Application.Settings;

public sealed record ServerChoice(string? SingleId, IReadOnlyList<string> ListedIds);
