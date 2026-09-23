using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.UnitTests.Application.Monitoring;

public class LatencyRangeTests
{
    [Theory]
    [InlineData("1h", 1, 60)]
    [InlineData("6h", 2, 180)]
    [InlineData("24h", 5, 288)]
    [InlineData("7d", 30, 336)]
    [InlineData("30d", 120, 360)]
    public void EveryRangeFitsInAFewHundredPoints(string id, int slotMinutes, int points)
    {
        var range = MonitoringService.RangeFor(id);

        Assert.Equal(slotMinutes, range.SlotMinutes);
        Assert.Equal(points, range.Window.TotalMinutes / range.SlotMinutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("last tuesday")]
    public void AnythingElseFallsBackToADay(string? id)
    {
        Assert.Equal("24h", MonitoringService.RangeFor(id).Id);
    }
}
