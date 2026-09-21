using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Integrations;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Infrastructure.SpeedTest;
using SpeedtestWatcher.Web.Background;

namespace SpeedtestWatcher.Tests;

public class SchedulerRetryTests : IDisposable
{
    private const string AlreadyRunning = "Speedtest is already running";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    [Fact(Timeout = 15000)]
    public async Task ARetryKeepsTheRun_SoACompetingRunCannotSlipIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        var pauseState = new ObservablePauseState();
        var scheduler = await BuildSchedulerAsync(runner, pauseState);

        var competing = new List<Task<SpeedtestExecutionResult>>();
        pauseState.OnFirstMarkedIdle = () => competing.Add(scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken));

        var result = await scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken);
        await Task.WhenAll(competing);

        Assert.True(result.Success, result.Error);
        Assert.Equal(1, runner.MostAtOnce);
    }

    [Fact(Timeout = 15000)]
    public async Task AfterARetry_TheNextRunCanStart_AndOnlyOneRunsAtATime()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        var scheduler = await BuildSchedulerAsync(runner, new ObservablePauseState());

        await scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken);
        var callsAfterFirstRun = runner.Calls;

        runner.HoldNextRun();
        var holding = scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken);
        await runner.RunStarted;
        var overlapping = await scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken);
        runner.ReleaseHeldRun();
        var held = await holding;

        Assert.Equal(2, callsAfterFirstRun);
        Assert.True(held.Success, held.Error);
        Assert.Equal(AlreadyRunning, overlapping.Error);
        Assert.Equal(1, runner.MostAtOnce);
    }

    [Fact(Timeout = 15000)]
    public async Task ARetryUsesTheServerThatWasAskedFor()
    {
        var runner = new FailsFirstRunner();
        var scheduler = await BuildSchedulerAsync(runner, new ObservablePauseState());

        await scheduler.ExecuteSpeedtestAsync(TestType.Custom, "4242", TestContext.Current.CancellationToken);

        Assert.Equal(["4242", "4242"], runner.ServerIds);
    }

    private async Task<SpeedtestSchedulerService> BuildSchedulerAsync(ISpeedtestRunner runner, IPauseStateService pauseState)
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        services.AddHttpClient();
        services.AddDbContext<SpeedtestWatcherDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddIntegrations();
        services.AddSingleton<ServerListProvider>();
        services.AddScoped<ServerSelector>();
        services.AddScoped<ConnectivityChecker>();
        services.AddSingleton(runner);
        services.AddSingleton(pauseState);
        services.AddSingleton<SpeedtestSchedulerService>();
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>().Database.EnsureCreated();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsStore>();
            await settings.InsertDefaultsAsync();
            await settings.SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla", ["internetCheckEnabled"] = "false" });
        }

        return provider.GetRequiredService<SpeedtestSchedulerService>();
    }

    public void Dispose() => _connection.Dispose();

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource();
        source.SetResult();
        return source;
    }

    private sealed class FailsFirstRunner : ISpeedtestRunner
    {
        private readonly object _gate = new();
        private int _active;
        private TaskCompletionSource _release = CompletedSource();
        private TaskCompletionSource _started = CompletedSource();
        private bool _holdNextCall;

        public int Calls { get; private set; }
        public int MostAtOnce { get; private set; }
        public List<string?> ServerIds { get; } = [];
        public Task RunStarted => _started.Task;

        public void HoldNextRun()
        {
            _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _holdNextCall = true;
        }

        public void ReleaseHeldRun() => _release.TrySetResult();

        public async Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default)
        {
            int call;
            bool held;
            lock (_gate)
            {
                call = ++Calls;
                ServerIds.Add(serverId);
                MostAtOnce = Math.Max(MostAtOnce, ++_active);
                held = _holdNextCall;
                _holdNextCall = false;
            }

            if (held)
            {
                _started.TrySetResult();
                await _release.Task;
            }
            await Task.Delay(20, cancellationToken);

            lock (_gate) _active--;

            return call == 1
                ? new SpeedtestExecutionResult { Success = false, Error = "Network unreachable" }
                : new SpeedtestExecutionResult { Success = true, Ping = 10, Download = 100, Upload = 50 };
        }
    }

    private sealed class ObservablePauseState : IPauseStateService
    {
        public Action? OnFirstMarkedIdle { get; set; }
        public bool IsPaused => false;
        public bool IsRunning { get; private set; }
        public DateTime? ResumesAt => null;

        public event Action? OnStatusChanged
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        public void SetRunning(bool running)
        {
            IsRunning = running;
            if (running) return;

            var onIdle = OnFirstMarkedIdle;
            OnFirstMarkedIdle = null;
            onIdle?.Invoke();
        }

        public void Pause(double? hours) => throw new NotSupportedException();

        public void Resume() => throw new NotSupportedException();
    }
}
