using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Monitoring;

internal sealed partial class ConnectivityMonitorService : BackgroundService
{
    private static readonly TimeSpan _idleWait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _betweenReconnectTests = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly IConnectionProbe _probe;
    private readonly ConnectionState _state;
    private readonly RunState _runState;
    private readonly IAppEvents _events;
    private readonly TimeProvider _time;
    private readonly ILogger<ConnectivityMonitorService> _logger;
    private readonly OutageTracker _tracker = new();

    private CancellationTokenSource _settingsChanged = new();
    private int? _sessionId;
    private DateTime? _lastReconnectTest;

    public ConnectivityMonitorService(
        IServiceScopeFactory scopes,
        IConnectionProbe probe,
        ConnectionState state,
        RunState runState,
        IAppEvents events,
        TimeProvider time,
        ILogger<ConnectivityMonitorService> logger)
    {
        _scopes = scopes;
        _probe = probe;
        _state = state;
        _runState = runState;
        _events = events;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _events.SettingsChanged += OnSettingsChanged;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = await ReadSettingsAsync(stoppingToken);
                try
                {
                    if (settings.Watching) await RoundAsync(settings, stoppingToken);
                    else StopWatching();
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogRoundFailed(ex);
                }

                await BackgroundDelay.WaitUntilAsync(
                    _time.GetUtcNow().UtcDateTime + (settings.Watching ? settings.Interval : _idleWait),
                    _time,
                    stoppingToken,
                    _settingsChanged.Token);
            }
        }
        finally
        {
            _events.SettingsChanged -= OnSettingsChanged;
            StopWatching();
        }
    }

    public override void Dispose()
    {
        _settingsChanged.Dispose();
        base.Dispose();
    }

    private async Task RoundAsync(MonitoringSettings settings, CancellationToken stoppingToken)
    {
        var duringTest = _runState.IsRunning;
        var result = await _probe.ProbeAsync(settings.Targets, MonitoringSettings.ProbeTimeout, stoppingToken);
        var at = _time.GetUtcNow().UtcDateTime;

        using var scope = _scopes.CreateScope();
        var monitoring = scope.ServiceProvider.GetRequiredService<IMonitoringRepository>();

        _sessionId ??= (await monitoring.StartWatchingAsync(at, stoppingToken)).Id;
        await monitoring.AddRoundAsync(new ProbeRound
        {
            At = at,
            Passed = result.Passed,
            Answered = result.Answered,
            Asked = result.Asked,
            FastestMilliseconds = result.FastestMilliseconds,
            DuringTest = duringTest
        }, stoppingToken);
        if (!await monitoring.KeepWatchingAsync(_sessionId.Value, at, stoppingToken))
            _sessionId = (await monitoring.StartWatchingAsync(at, stoppingToken)).Id;

        if (duringTest && !result.Passed) return;

        var change = _tracker.Record(result.Passed, settings);
        _state.Update(_tracker.Health, at, result.FastestMilliseconds);
        if (change == ConnectionChange.None) return;

        await ChangedAsync(change, at, settings, scope.ServiceProvider, monitoring, stoppingToken);
    }

    private async Task ChangedAsync(
        ConnectionChange change,
        DateTime at,
        MonitoringSettings settings,
        IServiceProvider services,
        IMonitoringRepository monitoring,
        CancellationToken stoppingToken)
    {
        var integrations = services.GetRequiredService<IIntegrationDispatcher>();

        if (change == ConnectionChange.WentDown)
        {
            var since = at - TimeSpan.FromSeconds(settings.IntervalSeconds * (settings.RoundsToGoDown - 1));
            await monitoring.StartOutageAsync(since, stoppingToken);
            LogConnectionLost(since);
            await integrations.PublishAsync(new ConnectionLost(since), stoppingToken);
            return;
        }

        var outage = await monitoring.EndOpenOutageAsync(at, stoppingToken);
        var downtime = outage?.Length ?? TimeSpan.Zero;
        LogConnectionRestored(downtime.TotalSeconds);
        await integrations.PublishAsync(new ConnectionRestored(at, downtime), stoppingToken);

        if (settings.TestAfterReconnect) await TestAfterReconnectAsync(at, services, stoppingToken);
    }

    private async Task TestAfterReconnectAsync(DateTime at, IServiceProvider services, CancellationToken stoppingToken)
    {
        if (_lastReconnectTest is { } last && at - last < _betweenReconnectTests) return;

        _lastReconnectTest = at;
        LogTestingAfterReconnect();
        await services.GetRequiredService<SpeedtestRunService>().RunAsync(TestType.Auto, cancellationToken: stoppingToken);
    }

    private void StopWatching()
    {
        if (_sessionId == null && _tracker.Health == ConnectionHealth.Unknown) return;

        _sessionId = null;
        _tracker.Forget();
        _state.StopWatching();
    }

    private void OnSettingsChanged(IReadOnlyDictionary<string, string> changes)
    {
        if (changes.Keys.Any(key => key.StartsWith("monitoring", StringComparison.OrdinalIgnoreCase)))
            Interlocked.Exchange(ref _settingsChanged, new CancellationTokenSource()).Cancel();
    }

    private async Task<MonitoringSettings> ReadSettingsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopes.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<ISettingsStore>().GetAsync(stoppingToken)).Monitoring;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A connection probe round failed")]
    private partial void LogRoundFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The connection looks down since {Since}")]
    private partial void LogConnectionLost(DateTime since);

    [LoggerMessage(Level = LogLevel.Information, Message = "The connection is back after {Seconds}s")]
    private partial void LogConnectionRestored(double seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Running a speedtest after the connection came back")]
    private partial void LogTestingAfterReconnect();
}
