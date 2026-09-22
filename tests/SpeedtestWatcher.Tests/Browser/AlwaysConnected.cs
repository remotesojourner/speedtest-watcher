using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Tests.Browser;

internal sealed class AlwaysConnected : IConnectivityChecker
{
    public Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default) => Task.FromResult(PreTestCheck.Ok);
}
