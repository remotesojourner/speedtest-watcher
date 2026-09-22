using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public sealed class SpeedtestPagingTests : IDisposable
{
    private static readonly DateTime September10 = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();
    private readonly SpeedtestWatcherDbContext _db;
    private readonly SpeedtestRepository _repository;

    public SpeedtestPagingTests()
    {
        _db = _database.NewContext();
        _repository = new SpeedtestRepository(_db);
    }

    [Fact]
    public async Task ImportedOlderResults_ArePagedAfterTheNewerOnes_EvenThoughTheirIdsAreHigher()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        foreach (var daysAgo in new[] { 2, 1, 0 })
            await _repository.CreateAsync(Result(September10.AddDays(-daysAgo)), cancellationToken);
        await _repository.ImportTestsAsync([Result(September10.AddDays(-9)), Result(September10.AddDays(-8)), Result(September10.AddDays(-7))], cancellationToken);

        var firstPage = await _repository.ListTestsAsync(null, 3, cancellationToken: cancellationToken);
        var secondPage = await _repository.ListTestsAsync(firstPage[^1].Id, 3, cancellationToken: cancellationToken);
        var thirdPage = await _repository.ListTestsAsync(secondPage[^1].Id, 3, cancellationToken: cancellationToken);

        Assert.Equal([September10, September10.AddDays(-1), September10.AddDays(-2)], firstPage.Select(Created));
        Assert.Equal([September10.AddDays(-7), September10.AddDays(-8), September10.AddDays(-9)], secondPage.Select(Created));
        Assert.Empty(thirdPage);
    }

    [Fact]
    public async Task ResultsCreatedAtTheSameMoment_AppearExactlyOnceAcrossPages()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        for (var i = 0; i < 5; i++)
            await _repository.CreateAsync(Result(September10), cancellationToken);

        var firstPage = await _repository.ListTestsAsync(null, 2, cancellationToken: cancellationToken);
        var secondPage = await _repository.ListTestsAsync(firstPage[^1].Id, 2, cancellationToken: cancellationToken);
        var thirdPage = await _repository.ListTestsAsync(secondPage[^1].Id, 2, cancellationToken: cancellationToken);

        Assert.Equal([5, 4, 3, 2, 1], firstPage.Concat(secondPage).Concat(thirdPage).Select(test => test.Id));
    }

    [Fact]
    public async Task PagingFromAResultThatNoLongerExists_ReturnsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _repository.CreateAsync(Result(September10), cancellationToken);

        Assert.Empty(await _repository.ListTestsAsync(99, 10, cancellationToken: cancellationToken));
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    private static Speedtest Result(DateTime created) => new() { Ping = 10, Download = 100, Upload = 50, Created = created };

    private static DateTime Created(Speedtest test) => test.Created;
}
