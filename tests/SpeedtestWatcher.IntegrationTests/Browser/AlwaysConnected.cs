using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.IntegrationTests.Browser;

internal sealed class AlwaysConnected : IConnectivityChecker
{
    public Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default) => Task.FromResult(PreTestCheck.Ok);
}
