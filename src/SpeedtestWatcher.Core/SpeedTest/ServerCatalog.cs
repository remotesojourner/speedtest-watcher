namespace SpeedtestWatcher.Core.SpeedTest;

public sealed record ServerCatalog(string ListUrl, Func<string, IReadOnlyList<ServerInfo>> Parse);
