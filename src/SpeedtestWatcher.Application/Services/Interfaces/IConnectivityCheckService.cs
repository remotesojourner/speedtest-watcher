using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IConnectivityCheckService
{
    Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default);
}
