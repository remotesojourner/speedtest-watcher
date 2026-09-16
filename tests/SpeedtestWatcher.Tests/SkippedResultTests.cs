using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Repositories;

namespace SpeedtestWatcher.Tests;

public class SkippedResultTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SpeedtestWatcherDbContext _db;

    public SkippedResultTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SpeedtestWatcherDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SpeedtestWatcherDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetStatistics_CountsOnlyRealFailures_AndIgnoresSkippedTests()
    {
        var repo = new SpeedtestRepository(_db);
        var today = DateTime.UtcNow.Date;

        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = today.AddHours(1) });
        await repo.CreateAsync(new Speedtest { Status = "skipped", Error = "Public IP 1.2.3.4 is on the skip list", Created = today.AddHours(2) });
        await repo.CreateAsync(new Speedtest { Status = "failed", Error = "Network unreachable", Created = today.AddHours(3) });

        var dateStr = today.ToString("yyyy-MM-dd");
        var stats = await repo.GetStatisticsAsync(dateStr, dateStr);

        Assert.Equal(3, stats.Tests.Total);
        Assert.Equal(1, stats.Tests.Failed);

        Assert.Equal(10, stats.Ping?.Avg);
        Assert.Equal(100.0, stats.Download?.Avg);
        Assert.Equal(50.0, stats.Upload?.Avg);

        Assert.Equal(1, stats.Failed.Count(failed => failed));
        Assert.Equal(["Network unreachable"], stats.Errors.Where(e => !string.IsNullOrEmpty(e)));
    }

    [Fact]
    public async Task GetLatestCompleted_LooksPastFailedAndSkippedResults()
    {
        var repo = new SpeedtestRepository(_db);
        var now = DateTime.UtcNow;

        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = now.AddMinutes(-10) });
        await repo.CreateAsync(new Speedtest { Status = "failed", Error = "Network unreachable", Created = now.AddMinutes(-5) });
        await repo.CreateAsync(new Speedtest { Status = "skipped", Error = "Public IP 1.2.3.4 is on the skip list", Created = now });

        Assert.Equal("skipped", (await repo.GetLatestAsync())?.Status);

        var completed = await repo.GetLatestCompletedAsync();
        Assert.Equal(100.0, completed?.Download);
        Assert.Equal("completed", completed?.Status);
    }

    [Fact]
    public async Task ListTests_FiltersByStatus()
    {
        var repo = new SpeedtestRepository(_db);
        var now = DateTime.UtcNow;

        await repo.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = now.AddMinutes(-10) });
        await repo.CreateAsync(new Speedtest { Status = "skipped", Error = "Public IP 1.2.3.4 is on the skip list", Created = now });

        var skipped = await repo.ListTestsAsync(null, 10, status: "skipped");
        Assert.Single(skipped);
        Assert.Equal("skipped", skipped[0].Status);

        Assert.Empty(await repo.ListTestsAsync(null, 10, status: "failed"));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
