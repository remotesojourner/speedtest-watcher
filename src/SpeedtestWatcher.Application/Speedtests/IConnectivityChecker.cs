using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Application.Speedtests;

public interface IConnectivityChecker
{
    Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default);
}
