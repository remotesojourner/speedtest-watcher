using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public sealed class SpeedtestRunServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task ARetryKeepsTheRunSoACompetingRunCannotSlipIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        var runs = await RunsAsync(runner, cancellationToken);

        SpeedtestExecutionResult? competing = null;
        runner.BeforeRetry = async () => competing = await NewRuns().RunAsync(TestType.Custom, cancellationToken: cancellationToken);

        var result = await runs.RunAsync(TestType.Custom, cancellationToken: cancellationToken);

        Assert.True(result.Success, result.Error);
        Assert.Equal(SpeedtestRunService.AlreadyRunning, competing?.Error);
        Assert.Equal(1, runner.MostAtOnce);
    }

    [Fact(Timeout = 15000)]
    public async Task AfterARetryTheNextRunCanStartAndOnlyOneRunsAtATime()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        var runs = await RunsAsync(runner, cancellationToken);

        await runs.RunAsync(TestType.Custom, cancellationToken: cancellationToken);
        var callsAfterFirstRun = runner.Calls;

        runner.HoldNextRun();
        var holding = runs.RunAsync(TestType.Custom, cancellationToken: cancellationToken);
        await runner.RunStarted;
        var overlapping = await NewRuns().RunAsync(TestType.Custom, cancellationToken: cancellationToken);
        runner.ReleaseHeldRun();
        var held = await holding;

        Assert.Equal(2, callsAfterFirstRun);
        Assert.True(held.Success, held.Error);
        Assert.Equal(SpeedtestRunService.AlreadyRunning, overlapping.Error);
        Assert.Equal(1, runner.MostAtOnce);
    }

    [Fact(Timeout = 15000)]
    public async Task ARetryUsesTheServerThatWasAskedFor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        var runs = await RunsAsync(runner, cancellationToken);

        await runs.RunAsync(TestType.Custom, "4242", cancellationToken);

        Assert.Equal(["4242", "4242"], runner.ServerIds);
    }

    [Fact(Timeout = 15000)]
    public async Task AManualRunHoldsTheRunBeforeAnsweringAndSavesAHandStartedResult()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        runner.HoldNextRun();
        var runs = await RunsAsync(runner, cancellationToken);
        var state = _services!.GetRequiredService<RunState>();

        var started = await runs.StartManualRunAsync("4242", cancellationToken);
        var runningRightAway = state.IsRunning;
        await runner.RunStarted;
        var second = await NewRuns().StartManualRunAsync(null, cancellationToken);
        runner.ReleaseHeldRun();
        while (state.IsRunning) await Task.Delay(20, cancellationToken);

        Assert.True(started.Succeeded);
        Assert.True(runningRightAway);
        Assert.Equal((OperationOutcome.Conflict, SpeedtestRunService.AlreadyRunning), (second.Outcome, second.Message));
        using var scope = _services!.CreateScope();
        var saved = Assert.Single(await scope.ServiceProvider.GetRequiredService<ISpeedtestRepository>().ListAllAsync(cancellationToken));
        Assert.Equal((TestType.Custom, TestStatus.Completed), (saved.Type, saved.Status));
        Assert.Equal(["4242", "4242"], runner.ServerIds);
    }

    [Fact(Timeout = 15000)]
    public async Task ManualRunsNeedFullAccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        _services = await RunTestServices.BuildAsync(_database, runner, cancellationToken, access: FixedAccess.ReadOnly);

        var result = await NewRuns().StartManualRunAsync(null, cancellationToken);

        Assert.Equal(OperationOutcome.Denied, result.Outcome);
        Assert.Equal(0, runner.Calls);
    }

    [Fact(Timeout = 15000)]
    public async Task WhilePausedScheduledAndManualRunsWaitButARunCanStillBeForced()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var runner = new FailsFirstRunner();
        var runs = await RunsAsync(runner, cancellationToken);
        _services!.GetRequiredService<RunState>().Pause(null);

        var scheduled = await runs.RunAsync(TestType.Auto, cancellationToken: cancellationToken);
        var manual = await runs.StartManualRunAsync(null, cancellationToken);
        var custom = await runs.RunAsync(TestType.Custom, cancellationToken: cancellationToken);

        Assert.Equal("Speedtest is paused", scheduled.Error);
        Assert.Equal((OperationOutcome.Conflict, "Speedtests are paused"), (manual.Outcome, manual.Message));
        Assert.True(custom.Success, custom.Error);
    }

    private async Task<SpeedtestRunService> RunsAsync(ISpeedtestRunner runner, CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, runner, cancellationToken);
        return NewRuns();
    }

    private SpeedtestRunService NewRuns() => _services!.CreateScope().ServiceProvider.GetRequiredService<SpeedtestRunService>();

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

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
        public Func<Task>? BeforeRetry { get; set; }

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

            if (call == 2 && BeforeRetry is { } beforeRetry) await beforeRetry();

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
}
