namespace SpeedtestWatcher.Application.Models;

public sealed record ServerCatalog(string ListUrl, Func<string, IReadOnlyList<ServerInfo>> Parse);
