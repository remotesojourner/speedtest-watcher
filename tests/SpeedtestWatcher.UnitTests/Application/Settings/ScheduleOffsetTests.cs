using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests.Application.Settings;

public class ScheduleOffsetTests
{
    [Theory]
    [InlineData(60)]
    [InlineData(100)]
    public void AGapTooSmallForTheShortestOffsetGetsNone(int gapSeconds)
    {
        Assert.Null(ScheduleOffset.Within(TimeSpan.FromSeconds(gapSeconds)));
    }

    [Theory]
    [InlineData(300, 75)]
    [InlineData(900, 225)]
    [InlineData(3600, 300)]
    public void TheOffsetStaysWithinAQuarterOfTheGapAndFiveMinutes(int gapSeconds, int longestSeconds)
    {
        var gap = TimeSpan.FromSeconds(gapSeconds);

        Assert.Equal(TimeSpan.FromSeconds(longestSeconds), ScheduleOffset.LongestWithin(gap));
        for (var draw = 0; draw < 200; draw++)
        {
            var offset = ScheduleOffset.Within(gap);
            Assert.InRange(offset!.Value, ScheduleOffset.Shortest, TimeSpan.FromSeconds(longestSeconds));
        }
    }
}
