using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Statistics;

public sealed class StatisticsTimeZoneTests : IDisposable
{
    private static readonly DateTime MorningUtc = new(2026, 9, 14, 8, 5, 0, DateTimeKind.Utc);
    private static readonly DateTime EveningUtc = new(2026, 9, 14, 20, 35, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();
    private readonly SpeedtestWatcherDbContext _db;
    private readonly SpeedtestRepository _repository;

    public StatisticsTimeZoneTests()
    {
        _db = _database.NewContext();
        _repository = new SpeedtestRepository(_db);
    }

    [Theory]
    [InlineData("UTC", 8, 20)]
    [InlineData("America/New_York", 4, 16)]
    [InlineData("Asia/Kolkata", 13, 2)]
    public async Task HourlyAverages_UseTheHoursOfTheRequestedTimeZone(string zone, int morningHour, int eveningHour)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken);

        var stats = await StatisticsAsync("2026-09-13", "2026-09-15", zone, cancellationToken);

        Assert.Equal(new[] { morningHour, eveningHour }.Order(), stats.HourlyAverages.Where(hour => hour.Count > 0).Select(hour => hour.Hour).Order());
    }

    [Theory]
    [InlineData("UTC", 2)]
    [InlineData("Asia/Tokyo", 1)]
    [InlineData("America/Los_Angeles", 2)]
    public async Task ADayRange_StartsAndEndsAtMidnightInTheRequestedTimeZone(string zone, int expectedResults)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken);

        var stats = await StatisticsAsync("2026-09-14", "2026-09-14", zone, cancellationToken);

        Assert.Equal(expectedResults, stats.Tests.Total);
    }

    [Fact]
    public async Task ATimeWithoutAnOffset_IsReadInTheRequestedTimeZone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken);

        var stats = await StatisticsAsync("2026-09-14T00:00:00", "2026-09-14T12:00:00", "America/New_York", cancellationToken);

        Assert.Equal(1, stats.Tests.Total);
    }

    [Fact]
    public async Task ATimeWithAnOffset_IsReadAsThatExactMoment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken);

        var stats = await StatisticsAsync("2026-09-14T20:00:00Z", "2026-09-14T22:00:00+01:00", "Asia/Tokyo", cancellationToken);

        Assert.Equal(1, stats.Tests.Total);
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        await _repository.CreateAsync(new Speedtest { Ping = 10, Download = 100, Upload = 50, Created = MorningUtc }, cancellationToken);
        await _repository.CreateAsync(new Speedtest { Ping = 20, Download = 200, Upload = 60, Created = EveningUtc }, cancellationToken);
    }

    private async Task<SpeedtestStatistics> StatisticsAsync(string from, string to, string zone, CancellationToken cancellationToken)
    {
        var result = await new StatisticsService(_repository).GetAsync(from, to, zone, cancellationToken);
        Assert.True(result.Succeeded, $"{zone} isn't installed on this machine");
        return result.Value!;
    }
}
