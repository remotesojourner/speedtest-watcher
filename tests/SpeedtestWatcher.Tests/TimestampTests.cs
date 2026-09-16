using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Tests;

public class TimestampTests
{
    private static readonly DateTime ExpectedUtc =
        DateTime.SpecifyKind(new DateTime(2026, 9, 15, 2, 2, 35).AddTicks(5041350), DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-09-15T02:02:35.5041350")]
    [InlineData("2026-09-15T02:02:35.5041350Z")]
    [InlineData("2026-09-15T03:02:35.5041350+01:00")]
    public void ParseTimestamp_ReturnsLocalTimeForTheSameInstant(string value)
    {
        var parsed = FormatHelper.ParseTimestamp(value);

        Assert.Equal(DateTimeKind.Local, parsed.Kind);
        Assert.Equal(ExpectedUtc, parsed.ToUniversalTime());
    }

    [Fact]
    public void StoredToLocal_TreatsUnspecifiedKindAsUtc()
    {
        var stored = DateTime.SpecifyKind(ExpectedUtc, DateTimeKind.Unspecified);

        var local = FormatHelper.StoredToLocal(stored);

        Assert.Equal(DateTimeKind.Local, local.Kind);
        Assert.Equal(ExpectedUtc, local.ToUniversalTime());
    }

    [Fact]
    public void StoredToLocal_LeavesLocalValuesUntouched()
    {
        var local = new DateTime(2026, 9, 15, 3, 2, 35, DateTimeKind.Local);

        Assert.Equal(local, FormatHelper.StoredToLocal(local));
    }
}
