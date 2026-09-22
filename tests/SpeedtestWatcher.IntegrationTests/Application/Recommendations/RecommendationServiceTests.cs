using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Recommendations;

public sealed class RecommendationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly SpeedtestWatcherDbContext _db;
    private readonly SpeedtestRepository _results;
    private readonly RecommendationRepository _recommendations;
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _db = _database.NewContext();
        _results = new SpeedtestRepository(_db);
        _recommendations = new RecommendationRepository(_db);
        _service = new RecommendationService(_recommendations, _results);
    }

    [Fact]
    public async Task TheRecommendation_IsTheBestOfTheLastTenCompletedTests_AndOnlyChangesAreReported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        for (var i = 1; i <= 10; i++)
        {
            await _results.CreateAsync(new Speedtest { Ping = 10 + i, Download = 100 + i * 10, Upload = 50 + i * 5, Created = DateTime.UtcNow.AddMinutes(i) }, cancellationToken);
        }

        var recommendation = await _service.RecalculateAsync(cancellationToken);

        Assert.Equal((11, 200.0, 100.0), (recommendation!.Ping, recommendation.Download, recommendation.Upload));
        Assert.Null(await _service.RecalculateAsync(cancellationToken));
    }

    [Fact]
    public async Task NothingIsSaved_BeforeTenTestsHaveCompleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        for (var i = 1; i < RecommendationService.CompletedTestsNeeded; i++)
        {
            await _results.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = DateTime.UtcNow.AddMinutes(i) }, cancellationToken);
            Assert.Null(await _service.RecalculateAsync(cancellationToken));
        }

        Assert.False((await _service.GetAsync(cancellationToken)).Succeeded);
    }

    [Fact]
    public async Task ThePlaceholderOfEarlierVersions_IsRemoved_WhileFewerThanTenTestsHaveCompleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _results.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = DateTime.UtcNow }, cancellationToken);
        await _recommendations.SaveAsync(25, 100, 50, cancellationToken);

        await _service.RemovePlaceholderAsync(cancellationToken);

        Assert.Null(await _recommendations.GetAsync(cancellationToken));
    }

    [Fact]
    public async Task RealRecommendationsThatMatchThePlaceholder_AreKept()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        for (var i = 0; i < RecommendationService.CompletedTestsNeeded; i++)
        {
            await _results.CreateAsync(new Speedtest { Ping = 25, Download = 100, Upload = 50, Created = DateTime.UtcNow.AddMinutes(i) }, cancellationToken);
        }
        await _recommendations.SaveAsync(25, 100, 50, cancellationToken);

        await _service.RemovePlaceholderAsync(cancellationToken);

        Assert.NotNull(await _recommendations.GetAsync(cancellationToken));
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }
}
