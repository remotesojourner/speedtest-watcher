using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Events;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.IntegrationTests.Application.Services;

public sealed class RecommendationsUpdatedTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task CompletedTestsBeforeTheTenthStoreNoRecommendation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scheduler = await BuildSchedulerAsync(new RecordingDispatcher(), cancellationToken);

        for (var run = 1; run < RecommendationService.CompletedTestsNeeded; run++)
        {
            var result = await scheduler.RunAsync(TestType.Custom, cancellationToken: cancellationToken);
            Assert.True(result.Success, result.Error);
        }

        using var scope = _services!.CreateScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IRecommendationRepository>().GetAsync(cancellationToken));
    }

    [Fact(Timeout = 15000)]
    public async Task TheTenthCompletedTestPublishesTheNewRecommendationsAndUnchangedValuesPublishNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var published = new RecordingDispatcher();
        var scheduler = await BuildSchedulerAsync(published, cancellationToken);

        for (var run = 1; run <= 11; run++)
        {
            var result = await scheduler.RunAsync(TestType.Custom, cancellationToken: cancellationToken);
            Assert.True(result.Success, result.Error);
        }

        var update = Assert.Single(published.Events.OfType<RecommendationsUpdated>());
        Assert.Equal((10, 950.5, 115.25), (update.Recommendation.Ping, update.Recommendation.Download, update.Recommendation.Upload));

        var tenthFinished = published.Events.Select((integrationEvent, index) => (integrationEvent, index)).Where(e => e.integrationEvent is TestFinished).ElementAt(9).index;
        Assert.Same(update, published.Events[tenthFinished + 1]);
    }

    private async Task<SpeedtestRunService> BuildSchedulerAsync(IIntegrationDispatchService dispatcher, CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, new SteadyRunner(), cancellationToken, dispatcher);
        return _services.CreateScope().ServiceProvider.GetRequiredService<SpeedtestRunService>();
    }

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class SteadyRunner : IToolRunnerService
    {
        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeedtestExecutionResult { Success = true, Ping = 10, Download = 950.5, Upload = 115.25 });
    }
}
