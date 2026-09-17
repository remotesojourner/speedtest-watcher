using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            .ConfigureWarnings(warnings => warnings.Throw(
                CoreEventId.FirstWithoutOrderByAndFilterWarning,
                CoreEventId.RowLimitingOperationWithoutOrderByWarning))
            .Options;

        _db = new SpeedtestWatcherDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task ConfigRepository_InsertDefaults_And_ValidateInput()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repo = new ConfigRepository(_db);
        await repo.InsertDefaultsAsync(cancellationToken);

        var all = await repo.ListAllAsync(cancellationToken);
        Assert.NotEmpty(all);
        Assert.Equal("0 * * * *", await repo.GetValueAsync("cron", cancellationToken));

        Assert.NotNull(await repo.ValidateInputAsync("ping", "abc", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("ping", "25", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("cron", "0 * * * *", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("cron", "invalid-cron", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("provider", "ookla", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("provider", "unknown", cancellationToken));

        Assert.Null(await repo.ValidateInputAsync("serverMode", "random", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("serverMode", "sideways", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("serverListMode", "deny", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("serverListMode", "maybe", cancellationToken));

        Assert.Null(await repo.ValidateInputAsync("ooklaServerIds", "none", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("ooklaServerIds", "12345,6789", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("ooklaServerIds", "nearest", cancellationToken));

        Assert.Null(await repo.ValidateInputAsync("internetCheckEnabled", "false", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("internetCheckEnabled", "yes", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("internetCheckUrl", "https://icanhazip.com", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("internetCheckUrl", "icanhazip", cancellationToken));

        Assert.Null(await repo.ValidateInputAsync("skipIps", "none", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("skipIps", "203.0.113.9, 2a00:23c8:870c:bf00::1", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("skipIps", "my-router", cancellationToken));

        Assert.Null(await repo.ValidateInputAsync("chartRange", "24h", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("chartRange", "12h", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("dateFormat", "ymd", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("dateFormat", "iso", cancellationToken));

        Assert.Null(await repo.ValidateInputAsync("visitorAccess", "read", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("visitorAccess", "everything", cancellationToken));
        Assert.Null(await repo.ValidateInputAsync("authEnabled", "true", cancellationToken));
        Assert.NotNull(await repo.ValidateInputAsync("authEnabled", "on", cancellationToken));
    }

    [Fact]
    public async Task ConfigRepository_Defaults_HaveSignInOff_AndDropThePasswordSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _db.Configs.Add(new ConfigEntry { Key = "password", Value = "$2a$11$hash" });
        _db.Configs.Add(new ConfigEntry { Key = "passwordLevel", Value = "read" });
        await _db.SaveChangesAsync(cancellationToken);

        var repo = new ConfigRepository(_db);
        await repo.InsertDefaultsAsync(cancellationToken);

        Assert.Equal("false", await repo.GetValueAsync("authEnabled", cancellationToken));
        Assert.Equal("none", await repo.GetValueAsync("visitorAccess", cancellationToken));
        Assert.Null(await repo.GetValueAsync("password", cancellationToken));
        Assert.Null(await repo.GetValueAsync("passwordLevel", cancellationToken));
    }

    [Fact]
    public async Task SpeedtestRepository_CreateAndList_WorksWithPagination()
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
    public async Task SpeedtestRepository_GetStatistics_AggregatesAccurately()
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
        var stats = await repo.GetStatisticsAsync(dateStr, dateStr, TimeZoneInfo.Utc, cancellationToken);

        Assert.Equal(2, stats.Tests.Total);
        Assert.Equal(0, stats.Tests.Failed);
        Assert.Equal(15, stats.Ping?.Avg);
        Assert.Equal(150.0, stats.Download?.Avg);
        Assert.Equal(75.0, stats.Upload?.Avg);
    }

    [Fact]
    public async Task RecommendationRepository_CalculatesFromTop10Tests()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
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
            }, cancellationToken);
        }

        var recommendation = await recRepo.RecalculateAsync(cancellationToken);
        Assert.NotNull(recommendation);
        Assert.Equal(11, recommendation.Ping);
        Assert.Equal(200.0, recommendation.Download);
        Assert.Equal(100.0, recommendation.Upload);

        Assert.Null(await recRepo.RecalculateAsync(cancellationToken));
    }

    [Fact]
    public async Task RecommendationRepository_SavesNothingBeforeTenCompletedTests()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var speedtestRepo = new SpeedtestRepository(_db);
        var recRepo = new RecommendationRepository(_db);

        for (var i = 1; i < RecommendationRepository.CompletedTestsNeeded; i++)
        {
            await speedtestRepo.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = DateTime.UtcNow.AddMinutes(i) }, cancellationToken);
            Assert.Null(await recRepo.RecalculateAsync(cancellationToken));
        }

        Assert.Null(await recRepo.GetAsync(cancellationToken));
    }

    [Fact]
    public async Task RecommendationRepository_RemovesThePlaceholderOfEarlierVersions_WhileFewerThanTenTestsHaveCompleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var speedtestRepo = new SpeedtestRepository(_db);
        var recRepo = new RecommendationRepository(_db);
        await speedtestRepo.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = DateTime.UtcNow }, cancellationToken);
        await recRepo.SaveAsync(25, 100, 50, cancellationToken);

        await recRepo.RemovePlaceholderAsync(cancellationToken);

        Assert.Null(await recRepo.GetAsync(cancellationToken));
    }

    [Fact]
    public async Task RecommendationRepository_KeepsRealRecommendationsThatMatchThePlaceholder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var speedtestRepo = new SpeedtestRepository(_db);
        var recRepo = new RecommendationRepository(_db);
        for (var i = 0; i < RecommendationRepository.CompletedTestsNeeded; i++)
        {
            await speedtestRepo.CreateAsync(new Speedtest { Ping = 25, Download = 100, Upload = 50, Created = DateTime.UtcNow.AddMinutes(i) }, cancellationToken);
        }
        await recRepo.SaveAsync(25, 100, 50, cancellationToken);

        await recRepo.RemovePlaceholderAsync(cancellationToken);

        Assert.NotNull(await recRepo.GetAsync(cancellationToken));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
