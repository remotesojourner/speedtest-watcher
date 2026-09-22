using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Recommendations;

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

    private async Task<SpeedtestRunService> BuildSchedulerAsync(IIntegrationDispatcher dispatcher, CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, new SteadyRunner(), cancellationToken, dispatcher);
        return _services.CreateScope().ServiceProvider.GetRequiredService<SpeedtestRunService>();
    }

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class SteadyRunner : ISpeedtestRunner
    {
        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeedtestExecutionResult { Success = true, Ping = 10, Download = 950.5, Upload = 115.25 });
    }

    private sealed class RecordingDispatcher : IIntegrationDispatcher
    {
        private readonly object _gate = new();
        private readonly List<IntegrationEvent> _events = [];

        public List<IntegrationEvent> Events
        {
            get
            {
                lock (_gate) return _events.ToList();
            }
        }

        public IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; } = new Dictionary<string, IntegrationTypeSchemaDto>();

        public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            lock (_gate) _events.Add(integrationEvent);
            return Task.CompletedTask;
        }

        public Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
