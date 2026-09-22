using System.Globalization;
using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public class DisplayFormatTests
{
    [Fact]
    public void ConvertSpeedHandlesMbpsAndMBytes()
    {
        Assert.Equal(100.0, DisplayFormat.ConvertSpeed(100.0, "mbps"));
        Assert.Equal(12.5, DisplayFormat.ConvertSpeed(100.0, "mbytes"));
        Assert.Equal(0, DisplayFormat.ConvertSpeed(null, "mbps"));
    }

    [Fact]
    public void ToRelativeTimeFormatsCorrectly()
    {
        var now = DateTime.UtcNow;
        Assert.Equal("Just now", DisplayFormat.ToRelativeTime(now.AddSeconds(-20)));
        Assert.Equal("5 minutes ago", DisplayFormat.ToRelativeTime(now.AddMinutes(-5)));
        Assert.Equal("2 hours ago", DisplayFormat.ToRelativeTime(now.AddHours(-2)));
        Assert.Equal("3 days ago", DisplayFormat.ToRelativeTime(now.AddDays(-3)));
    }

    [Theory]
    [InlineData("dmy", "14/09")]
    [InlineData("mdy", "09/14")]
    [InlineData("ymd", "09-14")]
    public void DayAndMonthPatternFollowsTheDateFormatSetting(string dateFormat, string expected)
    {
        Assert.Equal(expected, new DateTime(2026, 9, 14).ToString(DisplayFormat.DayAndMonthPattern(dateFormat), CultureInfo.InvariantCulture));
    }
}
