namespace SpeedtestWatcher.Application.Configuration;

public sealed record ServerChoice(string? PinnedId, IReadOnlyList<string> ListedIds);
