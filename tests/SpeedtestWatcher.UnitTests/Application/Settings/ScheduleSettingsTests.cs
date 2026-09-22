using Cronos;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests.Application.Settings;

public class ScheduleSettingsTests
{
    private static readonly DateTime _midnight = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("0 * * * *", 720)]
    [InlineData("0,30 * * * *", 1440)]
    [InlineData("0 3 * * *", 30)]
    [InlineData("0 3 * * 1", 4)]
    public void RunsPerMonthCountsTheNextThirtyDays(string cron, int expected)
    {
        Assert.Equal(expected, new ScheduleSettings(cron, RandomOffset: false).RunsPerMonth(_midnight));
    }

    [Fact]
    public void AnExpressionThatIsNotCronIsRefused()
    {
        Assert.Throws<CronFormatException>(() => new ScheduleSettings("every so often", RandomOffset: false).RunsPerMonth(_midnight));
    }
}
