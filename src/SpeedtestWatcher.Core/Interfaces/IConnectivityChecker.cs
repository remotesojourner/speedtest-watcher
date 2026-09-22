using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Core.Interfaces;

public interface IConnectivityChecker
{
    Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default);
}
