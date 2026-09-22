using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.SpeedTest;

namespace SpeedtestWatcher.Core.Interfaces;

public interface IServerListProvider
{
    Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default);
}
