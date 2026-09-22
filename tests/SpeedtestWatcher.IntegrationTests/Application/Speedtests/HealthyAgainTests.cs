using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public sealed class HealthyAgainTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly RecordingDispatcher _published = new();
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task TheFirstHealthyResultAfterAnUnhealthyOneSaysSoOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await RunAllAsync([Unhealthy(), Healthy(), Healthy()], 3, cancellationToken);

        Assert.Single(_published.Events.OfType<TestUnhealthy>());
        var healthyAgain = Assert.Single(_published.Events.OfType<TestHealthyAgain>());
        Assert.Equal(900, healthyAgain.Result.Download);
    }

    [Fact(Timeout = 15000)]
    public async Task AFailedTestInBetweenDoesNotCountEitherWay()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await RunAllAsync([Unhealthy(), Failed(), Failed(), Healthy()], 3, cancellationToken);

        Assert.Single(_published.Events.OfType<TestFailed>());
        Assert.Single(_published.Events.OfType<TestHealthyAgain>());
    }

    [Fact(Timeout = 15000)]
    public async Task HealthyResultsWithoutAnUnhealthyOneBeforeThemSendNothingExtra()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await RunAllAsync([Healthy(), Healthy()], 2, cancellationToken);

        Assert.Empty(_published.Events.OfType<TestHealthyAgain>());
    }

    private async Task RunAllAsync(SpeedtestExecutionResult[] results, int runs, CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, new QueuedRunner(results), cancellationToken, _published);
        var runService = _services.CreateScope().ServiceProvider.GetRequiredService<SpeedtestRunService>();
        for (var run = 0; run < runs; run++) await runService.RunAsync(TestType.Custom, cancellationToken: cancellationToken);
    }

    private static SpeedtestExecutionResult Healthy() => new() { Success = true, Ping = 10, Download = 900, Upload = 100 };

    private static SpeedtestExecutionResult Unhealthy() => new() { Success = true, Ping = 10, Download = 50, Upload = 100 };

    private static SpeedtestExecutionResult Failed() => new() { Success = false, Error = "Ookla stopped with an error: boom" };

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class QueuedRunner(SpeedtestExecutionResult[] results) : ISpeedtestRunner
    {
        private readonly Queue<SpeedtestExecutionResult> _results = new(results);

        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            Task.FromResult(_results.Dequeue());
    }
}
