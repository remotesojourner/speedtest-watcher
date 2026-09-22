namespace SpeedtestWatcher.Application.Providers;

public interface IServerListProvider
{
    Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default);
}
