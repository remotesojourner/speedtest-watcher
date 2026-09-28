using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.IntegrationTests.Browser;

internal sealed class AlwaysConnected : IConnectivityCheckService
{
    public Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default) => Task.FromResult(PreTestCheck.Ok);
}
