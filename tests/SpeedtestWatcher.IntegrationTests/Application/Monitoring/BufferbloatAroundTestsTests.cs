using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Application.Monitoring;

public sealed class BufferbloatAroundTestsTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly OfflineProbe _probe = new(12);
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task ATestThatSlowsTheLineStoresHowMuchItSlowedIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var stored = await RunAsync(new SlowingRunner(_probe, loadedMilliseconds: 92), cancellationToken);

        Assert.Equal((80, 12, 92), (stored.Bufferbloat, stored.LatencyIdle, stored.LatencyLoaded));
    }

    [Fact(Timeout = 15000)]
    public async Task AFailedTestStoresNoBufferbloat()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var stored = await RunAsync(new FailingRunner(), cancellationToken);

        Assert.Equal((TestStatus.Failed, null, null, null), (stored.Status, stored.Bufferbloat, stored.LatencyIdle, stored.LatencyLoaded));
    }

    private async Task<Speedtest> RunAsync(ISpeedtestRunner runner, CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, runner, cancellationToken,
            configure: services => services.AddSingleton<IConnectionProbe>(_probe));

        using var scope = _services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SpeedtestRunService>().RunAsync(TestType.Custom, cancellationToken: cancellationToken);

        await using var db = _database.NewContext();
        return Assert.Single(await new SpeedtestRepository(db).ListAllAsync(cancellationToken));
    }

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class SlowingRunner : ISpeedtestRunner
    {
        private readonly OfflineProbe _probe;
        private readonly double _loadedMilliseconds;

        public SlowingRunner(OfflineProbe probe, double loadedMilliseconds)
        {
            _probe = probe;
            _loadedMilliseconds = loadedMilliseconds;
        }

        public async Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default)
        {
            _probe.Milliseconds = _loadedMilliseconds;
            await Task.Delay(250, cancellationToken);
            return new SpeedtestExecutionResult { Success = true, Ping = 12, Download = 900, Upload = 100, Time = 1 };
        }
    }

    private sealed class FailingRunner : ISpeedtestRunner
    {
        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeedtestExecutionResult { Success = false, Error = "Network unreachable" });
    }
}
