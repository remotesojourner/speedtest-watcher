namespace SpeedtestWatcher.Application.Models;

public sealed record LatencyRange(string Id, string Title, TimeSpan Window, int SlotMinutes);
