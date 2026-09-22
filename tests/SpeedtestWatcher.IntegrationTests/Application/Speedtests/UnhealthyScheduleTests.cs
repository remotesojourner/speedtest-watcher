using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public sealed class UnhealthyScheduleTests : IDisposable
{
    private const string NewYearsDay = "0 0 1 1 *";
    private const string EveryMinute = "* * * * *";

    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 22, 12, 0, 30, TimeSpan.Zero));
    private readonly SignallingRunner _runner = new();
    private ServiceProvider? _services;
    private SpeedtestSchedulerService? _scheduler;

    [Fact(Timeout = 15000)]
    public async Task AResultThatMissedItsTargetsBringsTheNextTestForward()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartAsync(EveryMinute, offset: false, cancellationToken, Completed(healthy: false));

        Assert.True(await RanWithinAsync(TimeSpan.FromMinutes(5), cancellationToken));
    }

    [Fact(Timeout = 15000)]
    public async Task AnOffsetThatWouldNotFitTheGapIsSkipped()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var started = _time.GetUtcNow();
        await StartAsync(EveryMinute, offset: true, cancellationToken, Completed(healthy: false));

        Assert.True(await RanWithinAsync(TimeSpan.FromMinutes(5), cancellationToken));
        Assert.True(_time.GetUtcNow() - started < TimeSpan.FromSeconds(60));
    }

    [Fact(Timeout = 15000)]
    public async Task AResultThatMetItsTargetsGoesBackToTheMainSchedule()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartAsync(EveryMinute, offset: false, cancellationToken, Completed(healthy: true));

        Assert.False(await RanWithinAsync(TimeSpan.FromMinutes(10), cancellationToken));
    }

    [Fact(Timeout = 15000)]
    public async Task ASkippedResultLeavesTheScheduleAsItWas()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartAsync(EveryMinute, offset: false, cancellationToken, Completed(healthy: false), Skipped());

        Assert.True(await RanWithinAsync(TimeSpan.FromMinutes(5), cancellationToken));
    }

    [Fact(Timeout = 15000)]
    public async Task WithoutAnUnhealthyScheduleAMissedTargetChangesNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartAsync(unhealthyCron: null, offset: false, cancellationToken, Completed(healthy: false));

        Assert.False(await RanWithinAsync(TimeSpan.FromMinutes(10), cancellationToken));
    }

    private async Task StartAsync(string? unhealthyCron, bool offset, CancellationToken cancellationToken, params Speedtest[] results)
    {
        _services = await RunTestServices.BuildAsync(_database, _runner, cancellationToken, configure: services =>
        {
            services.AddSingleton<TimeProvider>(_time);
            services.AddSingleton(Options.Create(new SpeedtestWatcherOptions()));
        });

        using (var scope = _services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ISpeedtestRepository>();
            foreach (var result in results)
            {
                await repository.CreateAsync(result, cancellationToken);
            }
        }

        await SaveAsync(new Dictionary<string, string>
        {
            ["cron"] = NewYearsDay,
            ["scheduleOffset"] = offset ? "true" : "false",
            ["unhealthyCron"] = unhealthyCron ?? SettingDefinitions.Unset
        }, cancellationToken);

        _scheduler = ActivatorUtilities.CreateInstance<SpeedtestSchedulerService>(_services);
        await _scheduler.StartAsync(cancellationToken);
    }

    private async Task<bool> RanWithinAsync(TimeSpan window, CancellationToken cancellationToken)
    {
        var until = _time.GetUtcNow() + window;
        while (!_runner.Ran.IsCompleted && _time.GetUtcNow() < until)
        {
            _time.Advance(TimeSpan.FromSeconds(5));
            await Task.WhenAny(_runner.Ran, Task.Delay(25, cancellationToken));
        }

        await _scheduler!.StopAsync(cancellationToken);
        return _runner.Ran.IsCompleted;
    }

    private async Task SaveAsync(Dictionary<string, string> changes, CancellationToken cancellationToken)
    {
        using var scope = _services!.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<SettingsService>().SaveAsync(changes, cancellationToken)).Succeeded);
    }

    private Speedtest Completed(bool healthy) => new()
    {
        ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
        Ping = 12, Jitter = 0.4, Download = healthy ? 941.25 : 12.5, Upload = 110.5, Time = 14,
        Status = TestStatus.Completed, Healthy = healthy, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
        Type = TestType.Auto, Created = _time.GetUtcNow().UtcDateTime.AddMinutes(-10)
    };

    private Speedtest Skipped() => new()
    {
        Ping = -1, Download = -1, Upload = -1,
        Status = TestStatus.Skipped, Type = TestType.Auto, Error = "No internet connection",
        Created = _time.GetUtcNow().UtcDateTime.AddMinutes(-5)
    };

    public void Dispose()
    {
        _scheduler?.Dispose();
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class SignallingRunner : ISpeedtestRunner
    {
        private readonly TaskCompletionSource _ran = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ran => _ran.Task;

        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default)
        {
            _ran.TrySetResult();
            return Task.FromResult(new SpeedtestExecutionResult { Success = true, Ping = 10, Download = 100, Upload = 50 });
        }
    }
}
