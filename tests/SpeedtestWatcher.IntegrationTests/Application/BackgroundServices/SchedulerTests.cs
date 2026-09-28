using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Application.BackgroundServices;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Utils;

namespace SpeedtestWatcher.IntegrationTests.Application.BackgroundServices;

public sealed class SchedulerTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly WaitAwareTimeProvider _time = new(new DateTimeOffset(2026, 9, 22, 12, 0, 30, TimeSpan.Zero));
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task AScheduleMonthsAwayIsWaitedForInStepsInsteadOfFailing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var interrupted = new CancellationTokenSource();

        var waiting = BackgroundDelay.WaitUntilAsync(DateTime.UtcNow.AddDays(400), TimeProvider.System, cancellationToken, interrupted.Token);
        await Task.Delay(100, cancellationToken);
        await interrupted.CancelAsync();

        Assert.False(await waiting);
    }

    [Fact(Timeout = 15000)]
    public async Task ANewScheduleIsUsedStraightAwayInsteadOfAfterTheOldNextRun()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new SignallingRunner();
        _services = await RunTestServices.BuildAsync(_database, runner, cancellationToken, configure: services =>
        {
            services.AddSingleton<TimeProvider>(_time);
            services.AddSingleton(Options.Create(new SpeedtestWatcherOptions()));
        });
        await SaveAsync(new Dictionary<string, string> { ["cron"] = "0 0 1 1 *", ["scheduleOffset"] = "false" }, cancellationToken);

        using var scheduler = ActivatorUtilities.CreateInstance<SpeedtestSchedulerService>(_services);
        await scheduler.StartAsync(cancellationToken);
        await _time.NextWaitAsync(cancellationToken);
        await SaveAsync(new Dictionary<string, string> { ["cron"] = "* * * * *" }, cancellationToken);

        var ran = await _time.SkipWaitsUntilAsync(runner.Ran, TimeSpan.FromMinutes(1), cancellationToken);

        await scheduler.StopAsync(cancellationToken);
        Assert.True(ran);
    }

    [Fact(Timeout = 15000)]
    public async Task SkippingTheNextTestPassesOverOneScheduledRunAndKeepsTheOneAfter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new SignallingRunner();
        _services = await RunTestServices.BuildAsync(_database, runner, cancellationToken, configure: services =>
        {
            services.AddSingleton<TimeProvider>(_time);
            services.AddSingleton(Options.Create(new SpeedtestWatcherOptions()));
        });
        await SaveAsync(new Dictionary<string, string> { ["cron"] = "* * * * *", ["scheduleOffset"] = "false" }, cancellationToken);
        var state = _services.GetRequiredService<RunStateService>();
        state.SkipNextScheduledRun();

        using var scheduler = ActivatorUtilities.CreateInstance<SpeedtestSchedulerService>(_services);
        await scheduler.StartAsync(cancellationToken);
        await _time.SkipNextWaitAsync(cancellationToken);
        var untilTheRunAfter = await _time.NextWaitAsync(cancellationToken);

        Assert.False(state.IsPaused);
        Assert.False(runner.Ran.IsCompleted);

        _time.Advance(untilTheRunAfter);
        await runner.Ran.WaitAsync(cancellationToken);

        await scheduler.StopAsync(cancellationToken);
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 12, 2, 0, TimeSpan.Zero), _time.GetUtcNow());
    }

    private async Task SaveAsync(Dictionary<string, string> changes, CancellationToken cancellationToken)
    {
        using var scope = _services!.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<SettingsService>().SaveAsync(changes, cancellationToken)).Succeeded);
    }

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class SignallingRunner : IToolRunnerService
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
