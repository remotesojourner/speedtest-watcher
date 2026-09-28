using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IServerListService
{
    Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default);
}
