namespace SpeedtestWatcher.Application.Models;

public sealed record ServerInfo(string Id, string Name, string? Sponsor = null, string? Country = null, double? Distance = null, string? Host = null);
