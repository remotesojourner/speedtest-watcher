namespace SpeedtestWatcher.Core.Settings;

public sealed record ServerChoice(string? SingleId, IReadOnlyList<string> ListedIds);
