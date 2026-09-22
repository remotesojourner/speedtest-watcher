namespace SpeedtestWatcher.Application.Providers;

public sealed record ServerCatalog(string ListUrl, Func<string, IReadOnlyList<ServerInfo>> Parse);
