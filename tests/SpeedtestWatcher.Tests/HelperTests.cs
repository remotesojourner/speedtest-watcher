using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Tests;

public class HelperTests
{
    [Theory]
    [InlineData(100, 100, true, SpeedQuality.Green)]
    [InlineData(80, 100, true, SpeedQuality.Green)]
    [InlineData(74, 100, true, SpeedQuality.Orange)]
    [InlineData(30, 100, true, SpeedQuality.Orange)]
    [InlineData(29, 100, true, SpeedQuality.Red)]
    [InlineData(-1, 100, true, SpeedQuality.Error)]
    [InlineData(25, 25, false, SpeedQuality.Green)]
    [InlineData(30, 25, false, SpeedQuality.Green)]
    [InlineData(33, 25, false, SpeedQuality.Orange)]
    [InlineData(45, 25, false, SpeedQuality.Red)]
    public void SpeedQualityHelper_CalculatesCorrectQuality(double current, double optimal, bool higherIsBetter, SpeedQuality expected)
    {
        var quality = SpeedQualityHelper.GetQuality(current, optimal, higherIsBetter);
        Assert.Equal(expected, quality);
    }

    [Fact]
    public void FormatHelper_ConvertSpeed_HandlesMbpsAndMBytes()
    {
        Assert.Equal(100.0, FormatHelper.ConvertSpeed(100.0, "mbps"));
        Assert.Equal(12.5, FormatHelper.ConvertSpeed(100.0, "mbytes"));
        Assert.Equal(0, FormatHelper.ConvertSpeed(null, "mbps"));
    }

    [Fact]
    public void FormatHelper_ToRelativeTime_FormatsCorrectly()
    {
        var now = DateTime.UtcNow;
        Assert.Equal("Just now", FormatHelper.ToRelativeTime(now.AddSeconds(-20)));
        Assert.Equal("5 minutes ago", FormatHelper.ToRelativeTime(now.AddMinutes(-5)));
        Assert.Equal("2 hours ago", FormatHelper.ToRelativeTime(now.AddHours(-2)));
        Assert.Equal("3 days ago", FormatHelper.ToRelativeTime(now.AddDays(-3)));
    }

    [Fact]
    public void TemplateHelper_ReplacesVariablesCorrectly()
    {
        var vars = new Dictionary<string, string>
        {
            ["ping"] = "15",
            ["download"] = "250.50",
            ["upload"] = "50.25",
            ["error"] = "Connection refused"
        };

        var template = "Ping: %ping% ms, Down: %download% Mbps, Up: %upload% Mbps, Error: %error%";
        var result = TemplateHelper.ReplaceVariables(template, vars);

        Assert.Equal("Ping: 15 ms, Down: 250.50 Mbps, Up: 50.25 Mbps, Error: Connection refused", result);
    }
}
