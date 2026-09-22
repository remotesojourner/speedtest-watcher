namespace SpeedtestWatcher.Application.Settings;

public sealed record ServerChoice(string? PinnedId, IReadOnlyList<string> ListedIds);
