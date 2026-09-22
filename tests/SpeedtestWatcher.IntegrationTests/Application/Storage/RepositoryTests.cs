using System.Globalization;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Storage;

public sealed class RepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly SpeedtestWatcherDbContext _db;

    public RepositoryTests()
    {
        _db = _database.NewContext();
    }

    [Fact]
    public async Task SpeedtestRepositoryCreateAndListWorksWithPagination()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repo = new SpeedtestRepository(_db);

        for (var i = 1; i <= 25; i++)
        {
            await repo.CreateAsync(new Speedtest
            {
                Ping = 10 + i,
                Download = 100 + i,
                Upload = 50 + i,
                Time = 15,
                Created = DateTime.UtcNow.AddMinutes(i)
            }, cancellationToken);
        }

        var firstPage = await repo.ListTestsAsync(null, 10, cancellationToken: cancellationToken);
        Assert.Equal(10, firstPage.Count);
        Assert.True(firstPage[0].Id > firstPage[9].Id);

        var secondPage = await repo.ListTestsAsync(firstPage[9].Id, 10, cancellationToken: cancellationToken);
        Assert.Equal(10, secondPage.Count);
        Assert.True(secondPage[0].Id < firstPage[9].Id);
    }

    [Fact]
    public async Task SpeedtestRepositoryGetStatisticsAggregatesAccurately()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repo = new SpeedtestRepository(_db);
        var today = DateTime.UtcNow.Date;

        await repo.CreateAsync(new Speedtest
        {
            Ping = 10,
            Download = 100,
            Upload = 50,
            Created = today.AddHours(2)
        }, cancellationToken);

        await repo.CreateAsync(new Speedtest
        {
            Ping = 20,
            Download = 200,
            Upload = 100,
            Created = today.AddHours(4)
        }, cancellationToken);

        var dateStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var stats = (await new StatisticsService(repo).GetAsync(dateStr, dateStr, null, cancellationToken)).Value!;

        Assert.Equal(2, stats.Tests.Total);
        Assert.Equal(0, stats.Tests.Failed);
        Assert.Equal(15, stats.Ping?.Avg);
        Assert.Equal(150.0, stats.Download?.Avg);
        Assert.Equal(75.0, stats.Upload?.Avg);
    }

    [Fact]
    public async Task DataUsedCountsOnlyTheTestsInThePeriod()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repo = new SpeedtestRepository(_db);
        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 900, Upload = 100, DownloadBytes = 1000, UploadBytes = 100, Created = DateTime.UtcNow.AddDays(-2) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 900, Upload = 100, DownloadBytes = 20, UploadBytes = 3, Created = DateTime.UtcNow }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, Created = DateTime.UtcNow }, cancellationToken);

        Assert.Equal(1123, await repo.SumBytesSinceAsync(null, cancellationToken));
        Assert.Equal(23, await repo.SumBytesSinceAsync(DateTime.UtcNow.AddHours(-24), cancellationToken));
    }

    [Fact]
    public async Task TheRecentRunsBehindAnEstimateAreTheCompletedOnesThatReportedBytes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repo = new SpeedtestRepository(_db);
        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 900, Upload = 100, DownloadBytes = 1000, UploadBytes = 100, Created = DateTime.UtcNow.AddMinutes(-2) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 900, Upload = 100, Created = DateTime.UtcNow.AddMinutes(-1) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, DownloadBytes = 5, Created = DateTime.UtcNow }, cancellationToken);

        Assert.Equal([1100L], await repo.RecentRunBytesAsync(10, cancellationToken));
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }
}
