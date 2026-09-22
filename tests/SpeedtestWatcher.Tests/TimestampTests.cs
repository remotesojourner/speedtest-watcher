using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Web.Helpers;

namespace SpeedtestWatcher.Tests;

public class TimestampTests
{
    private static readonly DateTime ExpectedUtc =
        DateTime.SpecifyKind(new DateTime(2026, 9, 15, 2, 2, 35).AddTicks(5041350), DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-09-15T02:02:35.5041350")]
    [InlineData("2026-09-15T02:02:35.5041350Z")]
    [InlineData("2026-09-15T03:02:35.5041350+01:00")]
    public void ParseUtcTimestamp_ReturnsTheSameInstantInUtc(string value)
    {
        var parsed = TimeZones.ParseUtcTimestamp(value);

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(ExpectedUtc, parsed);
    }

    [Fact]
    public void AsUtc_TreatsUnspecifiedKindAsUtc()
    {
        var stored = DateTime.SpecifyKind(ExpectedUtc, DateTimeKind.Unspecified);

        Assert.Equal(ExpectedUtc, TimeZones.AsUtc(stored));
        Assert.Equal(DateTimeKind.Utc, TimeZones.AsUtc(stored).Kind);
    }

    [Theory]
    [InlineData("America/New_York", 22, 14)]
    [InlineData("Asia/Tokyo", 11, 15)]
    [InlineData("UTC", 2, 15)]
    public void InTimeZone_ShowsTheWallClockOfThatZone(string zone, int expectedHour, int expectedDay)
    {
        Assert.True(TimeZones.TryFindTimeZone(zone, out var timeZone));

        var local = TimeZones.InTimeZone(DateTime.SpecifyKind(ExpectedUtc, DateTimeKind.Unspecified), timeZone);

        Assert.Equal((expectedDay, expectedHour), (local.Day, local.Hour));
    }

    [Fact]
    public void WallClockToUtc_MovesPastTimesThatDaylightSavingSkips()
    {
        Assert.True(TimeZones.TryFindTimeZone("Europe/London", out var london));

        var utc = TimeZones.WallClockToUtc(new DateTime(2026, 3, 29, 1, 30, 0), london);

        Assert.Equal(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void TryFindTimeZone_RefusesUnknownNames_AndFallsBackToUtc()
    {
        Assert.False(TimeZones.TryFindTimeZone("Mars/Olympus_Mons", out var timeZone));
        Assert.Equal(TimeZoneInfo.Utc, timeZone);
    }

    [Theory]
    [InlineData("dmy", "14/09")]
    [InlineData("mdy", "09/14")]
    [InlineData("ymd", "09-14")]
    public void DayAndMonthPattern_FollowsTheDateFormatSetting(string dateFormat, string expected)
    {
        Assert.Equal(expected, new DateTime(2026, 9, 14).ToString(DisplayFormat.DayAndMonthPattern(dateFormat), System.Globalization.CultureInfo.InvariantCulture));
    }
}
