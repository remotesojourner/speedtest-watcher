using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public sealed class SchedulerTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 22, 12, 0, 30, TimeSpan.Zero));
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task AScheduleMonthsAway_IsWaitedForInSteps_InsteadOfFailing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var interrupted = new CancellationTokenSource();

        var waiting = BackgroundDelay.WaitUntilAsync(DateTime.UtcNow.AddDays(400), TimeProvider.System, cancellationToken, interrupted.Token);
        await Task.Delay(100, cancellationToken);
        await interrupted.CancelAsync();

        Assert.False(await waiting);
    }

    [Fact(Timeout = 15000)]
    public async Task ANewSchedule_IsUsedStraightAway_InsteadOfAfterTheOldNextRun()
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
        await Task.Delay(200, cancellationToken);
        await SaveAsync(new Dictionary<string, string> { ["cron"] = "* * * * *" }, cancellationToken);

        while (!runner.Ran.IsCompleted)
        {
            _time.Advance(TimeSpan.FromSeconds(31));
            await Task.WhenAny(runner.Ran, Task.Delay(100, cancellationToken));
        }

        await scheduler.StopAsync(cancellationToken);
        Assert.True(_time.GetUtcNow() < new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero));
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
