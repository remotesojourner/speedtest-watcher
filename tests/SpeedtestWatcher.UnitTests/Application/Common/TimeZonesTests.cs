using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.UnitTests.Application.Common;

public class TimeZonesTests
{
    private static readonly DateTime _expectedUtc =
        DateTime.SpecifyKind(new DateTime(2026, 9, 15, 2, 2, 35).AddTicks(5041350), DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-09-15T02:02:35.5041350")]
    [InlineData("2026-09-15T02:02:35.5041350Z")]
    [InlineData("2026-09-15T03:02:35.5041350+01:00")]
    public void ParseUtcTimestampReturnsTheSameInstantInUtc(string value)
    {
        var parsed = TimeZones.ParseUtcTimestamp(value);

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(_expectedUtc, parsed);
    }

    [Fact]
    public void AsUtcTreatsUnspecifiedKindAsUtc()
    {
        var stored = DateTime.SpecifyKind(_expectedUtc, DateTimeKind.Unspecified);

        Assert.Equal(_expectedUtc, TimeZones.AsUtc(stored));
        Assert.Equal(DateTimeKind.Utc, TimeZones.AsUtc(stored).Kind);
    }

    [Theory]
    [InlineData("America/New_York", 22, 14)]
    [InlineData("Asia/Tokyo", 11, 15)]
    [InlineData("UTC", 2, 15)]
    public void InTimeZoneShowsTheWallClockOfThatZone(string zone, int expectedHour, int expectedDay)
    {
        Assert.True(TimeZones.TryFindTimeZone(zone, out var timeZone));

        var local = TimeZones.InTimeZone(DateTime.SpecifyKind(_expectedUtc, DateTimeKind.Unspecified), timeZone);

        Assert.Equal((expectedDay, expectedHour), (local.Day, local.Hour));
    }

    [Fact]
    public void WallClockToUtcMovesPastTimesThatDaylightSavingSkips()
    {
        Assert.True(TimeZones.TryFindTimeZone("Europe/London", out var london));

        var utc = TimeZones.WallClockToUtc(new DateTime(2026, 3, 29, 1, 30, 0), london);

        Assert.Equal(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void TryFindTimeZoneRefusesUnknownNamesAndFallsBackToUtc()
    {
        Assert.False(TimeZones.TryFindTimeZone("Mars/Olympus_Mons", out var timeZone));
        Assert.Equal(TimeZoneInfo.Utc, timeZone);
    }
}
