using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Repositories;

namespace SpeedtestWatcher.Tests;

public class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SpeedtestWatcherDbContext _db;

    public RepositoryTests()
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
    public async Task ConfigRepository_InsertDefaults_And_ValidateInput()
    {
        var repo = new ConfigRepository(_db);
        await repo.InsertDefaultsAsync();

        var all = await repo.ListAllAsync();
        Assert.NotEmpty(all);
        Assert.Equal("0 * * * *", await repo.GetValueAsync("cron"));

        Assert.NotNull(await repo.ValidateInputAsync("ping", "abc"));
        Assert.Null(await repo.ValidateInputAsync("ping", "25"));
        Assert.Null(await repo.ValidateInputAsync("cron", "0 * * * *"));
        Assert.NotNull(await repo.ValidateInputAsync("cron", "invalid-cron"));
        Assert.Null(await repo.ValidateInputAsync("provider", "ookla"));
        Assert.NotNull(await repo.ValidateInputAsync("provider", "unknown"));

        Assert.Null(await repo.ValidateInputAsync("serverMode", "random"));
        Assert.NotNull(await repo.ValidateInputAsync("serverMode", "sideways"));
        Assert.Null(await repo.ValidateInputAsync("serverListMode", "deny"));
        Assert.NotNull(await repo.ValidateInputAsync("serverListMode", "maybe"));

        Assert.Null(await repo.ValidateInputAsync("ooklaServerIds", "none"));
        Assert.Null(await repo.ValidateInputAsync("ooklaServerIds", "12345,6789"));
        Assert.NotNull(await repo.ValidateInputAsync("ooklaServerIds", "nearest"));

        Assert.Null(await repo.ValidateInputAsync("internetCheckEnabled", "false"));
        Assert.NotNull(await repo.ValidateInputAsync("internetCheckEnabled", "yes"));
        Assert.Null(await repo.ValidateInputAsync("internetCheckUrl", "https://icanhazip.com"));
        Assert.NotNull(await repo.ValidateInputAsync("internetCheckUrl", "icanhazip"));

        Assert.Null(await repo.ValidateInputAsync("skipIps", "none"));
        Assert.Null(await repo.ValidateInputAsync("skipIps", "203.0.113.9, 2a00:23c8:870c:bf00::1"));
        Assert.NotNull(await repo.ValidateInputAsync("skipIps", "my-router"));

        Assert.Null(await repo.ValidateInputAsync("chartRange", "24h"));
        Assert.NotNull(await repo.ValidateInputAsync("chartRange", "12h"));
        Assert.Null(await repo.ValidateInputAsync("dateFormat", "ymd"));
        Assert.NotNull(await repo.ValidateInputAsync("dateFormat", "iso"));

        Assert.Null(await repo.ValidateInputAsync("visitorAccess", "read"));
        Assert.NotNull(await repo.ValidateInputAsync("visitorAccess", "everything"));
        Assert.Null(await repo.ValidateInputAsync("authEnabled", "true"));
        Assert.NotNull(await repo.ValidateInputAsync("authEnabled", "on"));
    }

    [Fact]
    public async Task ConfigRepository_Defaults_HaveSignInOff_AndDropThePasswordSettings()
    {
        _db.Configs.Add(new ConfigEntry { Key = "password", Value = "$2a$11$hash" });
        _db.Configs.Add(new ConfigEntry { Key = "passwordLevel", Value = "read" });
        await _db.SaveChangesAsync();

        var repo = new ConfigRepository(_db);
        await repo.InsertDefaultsAsync();

        Assert.Equal("false", await repo.GetValueAsync("authEnabled"));
        Assert.Equal("none", await repo.GetValueAsync("visitorAccess"));
        Assert.Null(await repo.GetValueAsync("password"));
        Assert.Null(await repo.GetValueAsync("passwordLevel"));
    }

    [Fact]
    public async Task SpeedtestRepository_CreateAndList_WorksWithPagination()
    {
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
            });
        }

        var firstPage = await repo.ListTestsAsync(null, 10);
        Assert.Equal(10, firstPage.Count);
        Assert.True(firstPage[0].Id > firstPage[9].Id);

        var secondPage = await repo.ListTestsAsync(firstPage[9].Id, 10);
        Assert.Equal(10, secondPage.Count);
        Assert.True(secondPage[0].Id < firstPage[9].Id);
    }

    [Fact]
    public async Task SpeedtestRepository_GetStatistics_AggregatesAccurately()
    {
        var repo = new SpeedtestRepository(_db);
        var today = DateTime.UtcNow.Date;

        await repo.CreateAsync(new Speedtest
        {
            Ping = 10,
            Download = 100,
            Upload = 50,
            Created = today.AddHours(2)
        });

        await repo.CreateAsync(new Speedtest
        {
            Ping = 20,
            Download = 200,
            Upload = 100,
            Created = today.AddHours(4)
        });

        var dateStr = today.ToString("yyyy-MM-dd");
        var stats = await repo.GetStatisticsAsync(dateStr, dateStr);

        Assert.Equal(2, stats.Tests.Total);
        Assert.Equal(0, stats.Tests.Failed);
        Assert.Equal(15, stats.Ping?.Avg);
        Assert.Equal(150.0, stats.Download?.Avg);
        Assert.Equal(75.0, stats.Upload?.Avg);
    }

    [Fact]
    public async Task RecommendationRepository_CalculatesFromTop10Tests()
    {
        var speedtestRepo = new SpeedtestRepository(_db);
        var recRepo = new RecommendationRepository(_db);

        for (var i = 1; i <= 10; i++)
        {
            await speedtestRepo.CreateAsync(new Speedtest
            {
                Ping = 10 + i,
                Download = 100 + i * 10,
                Upload = 50 + i * 5,
                Created = DateTime.UtcNow.AddMinutes(i)
            });
        }

        var rec = await recRepo.UpdateOrCalculateAsync();
        Assert.Equal(11, rec.Ping);
        Assert.Equal(200.0, rec.Download);
        Assert.Equal(100.0, rec.Upload);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
