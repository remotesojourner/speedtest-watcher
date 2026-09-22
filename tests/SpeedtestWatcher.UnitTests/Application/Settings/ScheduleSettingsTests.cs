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
    public void RunsInAMonthCountsTheNextThirtyDays(string cron, int expected)
    {
        Assert.Equal(expected, new ScheduleSettings(cron, RandomOffset: false).RunsIn(TimeSpan.FromDays(30), _midnight));
    }

    [Theory]
    [InlineData("*/5 * * * *", 12)]
    [InlineData("*/15 * * * *", 4)]
    [InlineData("0 * * * *", 1)]
    public void RunsInAnHourCountsTheNextHour(string cron, int expected)
    {
        Assert.Equal(expected, new ScheduleSettings(cron, RandomOffset: false).RunsIn(TimeSpan.FromHours(1), _midnight));
    }

    [Fact]
    public void AnExpressionThatIsNotCronIsRefused()
    {
        Assert.Throws<CronFormatException>(() => new ScheduleSettings("every so often", RandomOffset: false).RunsIn(TimeSpan.FromDays(30), _midnight));
    }

    [Fact]
    public void TheUnhealthyScheduleIsUsedOnlyWhenItIsSetAndTheLineIsUnhealthy()
    {
        var schedule = new ScheduleSettings("0 * * * *", RandomOffset: false, UnhealthyCron: "*/5 * * * *");

        Assert.Equal(_midnight.AddHours(1), schedule.NextRunAfter(_midnight));
        Assert.Equal(_midnight.AddMinutes(5), schedule.NextRunAfter(_midnight, unhealthy: true));
        Assert.True(schedule.HasUnhealthySchedule);
    }

    [Fact]
    public void WithoutAnUnhealthyScheduleTheMainOneAlwaysApplies()
    {
        var schedule = new ScheduleSettings("0 * * * *", RandomOffset: false);

        Assert.Equal(_midnight.AddHours(1), schedule.NextRunAfter(_midnight, unhealthy: true));
        Assert.False(schedule.HasUnhealthySchedule);
    }
}
