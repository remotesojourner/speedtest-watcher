using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Tests;

public class StatisticsServiceTests
{
    private static readonly DateTime Start = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EachResult_IsAChartPoint_WithReadingsOnlyForCompletedTests()
    {
        var rows = new List<Speedtest>
        {
            new() { Ping = 12, Jitter = 0.5, Download = 900, Upload = 90, Time = 15, Created = Start.AddHours(1) },
            new() { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, Error = "Network unreachable", Created = Start.AddHours(2) },
            new() { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Skipped, Error = "On the skip list", Created = Start.AddHours(3) }
        };

        var statistics = StatisticsService.Compute(rows, Range(days: 1));

        Assert.Equal(
            [
                new ChartPoint(Start.AddHours(1), false, null, 12, 0.5, 900, 90, 15),
                new ChartPoint(Start.AddHours(2), true, "Network unreachable", null, null, null, null, null),
                new ChartPoint(Start.AddHours(3), false, null, null, null, null, null, null)
            ],
            statistics.ChartPoints);
        Assert.Equal((3, 1, false), (statistics.Tests.Total, statistics.Tests.Failed, statistics.Downsampled));
    }

    [Fact]
    public void MoreResultsThanTheChartCanShow_AreAveragedIntoTimeBuckets()
    {
        var rows = Enumerable.Range(0, 400)
            .Select(i => new Speedtest { Ping = 10, Download = 100 + i % 2, Upload = 50, Created = Start.AddMinutes(i * 10) })
            .ToList();

        var statistics = StatisticsService.Compute(rows, Range(days: 3));

        Assert.True(statistics.Downsampled);
        Assert.Equal(400, statistics.RawDataPoints);
        Assert.InRange(statistics.ChartPoints.Count, 1, StatisticsService.MaxChartPoints);
        Assert.All(statistics.ChartPoints, point => Assert.InRange(point.Download!.Value, 100, 101));
    }

    private static StatisticsRange Range(int days) =>
        new("2026-09-14", "2026-09-14", Start, Start.AddDays(days).AddTicks(-1), TimeZoneInfo.Utc);
}
