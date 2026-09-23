using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.UnitTests.Application.Monitoring;

public class UptimeTests
{
    private static readonly DateTime _now = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UptimeIsTheWatchedTimeLessTheDowntime()
    {
        var uptime = Over(
            [Watched(_now.AddHours(-24), _now)],
            [Outage(_now.AddHours(-2), _now.AddHours(-1))]);

        Assert.Equal((95.833, 86400L, 3600L, 1), (uptime.Percent, uptime.WatchedSeconds, uptime.DownSeconds, uptime.Outages));
    }

    [Fact]
    public void TimeTheAppWasNotWatchingCountsNeitherWay()
    {
        var uptime = Over(
            [Watched(_now.AddHours(-24), _now.AddHours(-20)), Watched(_now.AddHours(-2), _now)],
            []);

        Assert.Equal((100, 6 * 3600L), (uptime.Percent, uptime.WatchedSeconds));
    }

    [Fact]
    public void AnOutageIsClippedToThePeriodAsked()
    {
        var uptime = Over(
            [Watched(_now.AddHours(-48), _now)],
            [Outage(_now.AddHours(-25), _now.AddHours(-23))]);

        Assert.Equal(3600L, uptime.DownSeconds);
    }

    [Fact]
    public void AnOutageStillRunningCountsUpToNow()
    {
        var uptime = Over(
            [Watched(_now.AddHours(-24), _now)],
            [new Outage { StartedAt = _now.AddMinutes(-30), EndedAt = null }]);

        Assert.Equal(1800L, uptime.DownSeconds);
    }

    [Fact]
    public void WithoutWatchedTimeThereIsNoPercentage()
    {
        var uptime = Over([], []);

        Assert.Null(uptime.Percent);
        Assert.Equal(0, uptime.WatchedSeconds);
    }

    [Fact]
    public void EachDayGetsItsOwnDowntime()
    {
        var days = Uptime.ByDay(3, [Outage(_now.AddDays(-1).AddHours(-1), _now.AddDays(-1))], TimeZoneInfo.Utc, _now);

        Assert.Equal([0, 3600, 0], days.Select(day => day.DownSeconds));
        Assert.Equal([0, 1, 0], days.Select(day => day.Outages));
        Assert.Equal(DateOnly.FromDateTime(_now), days[^1].Date);
    }

    [Fact]
    public void AnOutageAcrossMidnightIsSplitBetweenTheDays()
    {
        var days = Uptime.ByDay(3, [Outage(_now.AddDays(-1).Date.AddMinutes(-30), _now.AddDays(-1).Date.AddMinutes(30))], TimeZoneInfo.Utc, _now);

        Assert.Equal([1800, 1800, 0], days.Select(day => day.DownSeconds));
    }

    private static UptimeDto Over(WatchSession[] sessions, Outage[] outages) =>
        Uptime.Over(_now.AddHours(-24), _now, sessions, outages, _now);

    private static WatchSession Watched(DateTime from, DateTime to) => new() { StartedAt = from, LastSeenAt = to };

    private static Outage Outage(DateTime from, DateTime to) => new() { StartedAt = from, EndedAt = to };
}
