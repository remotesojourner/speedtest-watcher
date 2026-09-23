using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Monitoring;

public sealed class ConnectivityMonitorTests : IDisposable
{
    private const int IntervalSeconds = 5;

    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
    private readonly ScriptedProbe _probe = new();
    private readonly RecordingDispatcher _integrations = new();
    private ServiceProvider? _services;
    private ConnectivityMonitorService? _monitor;

    [Fact(Timeout = 20000)]
    public async Task ThreeFailedRoundsMakeOneOutageThatEndsWhenTheLineIsBack()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _probe.Script([false, false, false, true, true]);
        await StartAsync(cancellationToken);

        await RoundsAsync(5, cancellationToken);

        var outage = Assert.Single(await OutagesAsync(cancellationToken));
        Assert.Equal(_time.GetUtcNow().UtcDateTime.AddSeconds(-4 * IntervalSeconds), outage.StartedAt);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, outage.EndedAt);
        Assert.Equal(TimeSpan.FromSeconds(4 * IntervalSeconds), outage.Length);
        Assert.Equal(ConnectionHealth.Up, Connection.Current.Health);
    }

    [Fact(Timeout = 20000)]
    public async Task OneFailedRoundIsNotAnOutage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _probe.Script([true, false, true, true]);
        await StartAsync(cancellationToken);

        await RoundsAsync(4, cancellationToken);

        Assert.Empty(await OutagesAsync(cancellationToken));
        Assert.Equal(ConnectionHealth.Up, Connection.Current.Health);
    }

    [Fact(Timeout = 20000)]
    public async Task ALostConnectionIsAnnouncedAndSoIsItsReturn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _probe.Script([false, false, false, true, true]);
        await StartAsync(cancellationToken);

        await RoundsAsync(5, cancellationToken);

        var announced = _integrations.Events.Where(published => published is ConnectionLost or ConnectionRestored).ToList();

        Assert.Collection(announced,
            published => Assert.Equal(_time.GetUtcNow().UtcDateTime.AddSeconds(-4 * IntervalSeconds), Assert.IsType<ConnectionLost>(published).Since),
            published => Assert.Equal(TimeSpan.FromSeconds(4 * IntervalSeconds), Assert.IsType<ConnectionRestored>(published).Downtime));
    }

    [Fact(Timeout = 20000)]
    public async Task FailuresWhileASpeedtestRunsAreRecordedButStartNoOutage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _probe.Script([false, false, false, false]);
        await StartAsync(cancellationToken);
        _services!.GetRequiredService<RunState>().TryStartRun();

        await RoundsAsync(4, cancellationToken);

        Assert.Empty(await OutagesAsync(cancellationToken));
        await using var db = _database.NewContext();
        var rounds = await new MonitoringRepository(db).ListRoundsSinceAsync(DateTime.MinValue, cancellationToken);
        Assert.Equal(4, rounds.Count);
        Assert.All(rounds, round => Assert.True(round.DuringTest));
    }

    [Fact(Timeout = 20000)]
    public async Task WatchingIsRecordedSoUptimeKnowsWhatItCanCount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _probe.Script([true, true, true]);
        await StartAsync(cancellationToken);

        await RoundsAsync(3, cancellationToken);

        await using var db = _database.NewContext();
        var session = Assert.Single(await new MonitoringRepository(db).ListWatchSessionsSinceAsync(DateTime.MinValue, cancellationToken));
        Assert.Equal(TimeSpan.FromSeconds(2 * IntervalSeconds), session.LastSeenAt - session.StartedAt);
    }

    private ConnectionState Connection => _services!.GetRequiredService<ConnectionState>();

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, new UnusedRunner(), cancellationToken, dispatcher: _integrations, configure: services =>
        {
            services.AddSingleton<TimeProvider>(_time);
            services.AddSingleton<IConnectionProbe>(_probe);
        });

        using (var scope = _services.CreateScope())
        {
            var saved = await scope.ServiceProvider.GetRequiredService<SettingsService>().SaveAsync(new Dictionary<string, string>
            {
                ["monitoringEnabled"] = "true",
                ["monitoringTargets"] = "1.1.1.1:443",
                ["monitoringInterval"] = IntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["monitoringRoundsDown"] = "3",
                ["monitoringRoundsUp"] = "2"
            }, cancellationToken);
            Assert.True(saved.Succeeded);
        }

        _monitor = ActivatorUtilities.CreateInstance<ConnectivityMonitorService>(_services);
        await _monitor.StartAsync(cancellationToken);
    }

    private async Task RoundsAsync(int rounds, CancellationToken cancellationToken)
    {
        while (_probe.Rounds < rounds)
        {
            var before = _probe.Rounds;
            for (var wait = 0; wait < 50 && _probe.Rounds == before; wait++)
            {
                await Task.Delay(10, cancellationToken);
            }

            if (_probe.Rounds == before) _time.Advance(TimeSpan.FromSeconds(IntervalSeconds));
        }

        await Task.Delay(50, cancellationToken);
    }

    private async Task<List<Outage>> OutagesAsync(CancellationToken cancellationToken)
    {
        await using var db = _database.NewContext();
        return await new MonitoringRepository(db).ListOutagesSinceAsync(DateTime.MinValue, 10, cancellationToken);
    }

    public void Dispose()
    {
        _monitor?.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        _monitor?.Dispose();
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class ScriptedProbe : IConnectionProbe
    {
        private readonly Lock _gate = new();
        private readonly Queue<bool> _answers = new();
        public int Rounds { get; private set; }

        public void Script(bool[] answers)
        {
            lock (_gate)
            {
                foreach (var answer in answers) _answers.Enqueue(answer);
            }
        }

        public Task<ProbeResult> ProbeAsync(IReadOnlyList<ProbeTarget> targets, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            bool passed;
            lock (_gate)
            {
                passed = _answers.Count == 0 || _answers.Dequeue();
                Rounds++;
            }

            return Task.FromResult(new ProbeResult(passed ? targets.Count : 0, targets.Count, passed ? 12.5 : null));
        }
    }

    private sealed class UnusedRunner : ISpeedtestRunner
    {
        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
