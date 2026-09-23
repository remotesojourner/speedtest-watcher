using System.Globalization;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Web.Ui.Pages;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public class DashboardChartsTests
{
    private static readonly DateTime _start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ReadingsBecomeSeriesWithAnAverageInTheChosenUnit()
    {
        var charts = Build([Reading(0, download: 100, upload: 20, ping: 10, jitter: 2), Reading(1, download: 200, upload: 40, ping: 30, jitter: 4)],
            convertSpeed: mbps => mbps / 8);

        Assert.Equal(["08:00", "09:00"], charts.Labels);
        Assert.Equal([12.5, 25], charts.Download[0].Data.Values);
        Assert.Equal([18.75, 18.75], charts.Download[1].Data.Values);
        Assert.Equal([2.5, 5], charts.Upload[0].Data.Values);
        Assert.Equal(["Ping", "Jitter", "Average"], charts.Ping.Select(series => series.Name));
        Assert.Equal([10, 30], charts.Ping[0].Data.Values);
        Assert.Equal([2, 4], charts.Ping[1].Data.Values);
        Assert.Equal([20, 20], charts.Ping[2].Data.Values);
    }

    [Fact]
    public void BufferbloatHasItsOwnChartOfTheTestsThatMeasuredIt()
    {
        var charts = Build([
            Reading(0, download: 100),
            Reading(1, download: 100) with { BufferbloatDown = 5, BufferbloatUp = 40 },
            Reading(2, download: 100) with { BufferbloatUp = 60 }]);

        Assert.True(charts.HasBufferbloat);
        Assert.Equal(["Ping", "Jitter", "Average"], charts.Ping.Select(series => series.Name));
        Assert.Equal(["09:00", "10:00"], charts.BufferbloatLabels);
        Assert.Equal(["Download", "Upload"], charts.Bufferbloat.Select(series => series.Name));
        Assert.Equal([5, 0], charts.Bufferbloat[0].Data.Values);
        Assert.Equal([40, 60], charts.Bufferbloat[1].Data.Values);
        Assert.Equal(20, charts.TickSteps.Bufferbloat);
    }

    [Fact]
    public void APeriodWithoutBufferbloatHasNoBufferbloatChart()
    {
        var charts = Build([Reading(0, download: 100), Reading(1, download: 120)]);

        Assert.False(charts.HasBufferbloat);
        Assert.Empty(charts.Bufferbloat);
    }

    [Fact]
    public void FailedTestsAreLeftOutOfTheChartsAndListedNewestFirst()
    {
        var charts = Build([Failure(0, "no-internet"), Reading(1, download: 100), Failure(2, "timeout"), Reading(3, download: null)]);

        Assert.Equal(["09:00"], charts.Labels);
        Assert.Equal(["timeout", "no-internet"], charts.Failures.Select(point => point.Error));
    }

    [Fact]
    public void TickStepsFollowTheRangeOfEachChartWithJitterOnThePingChart()
    {
        var points = new[] { Reading(0, download: 100, upload: 10, ping: 10, jitter: 2), Reading(1, download: 900, upload: 50, ping: 12, jitter: 3) };

        var charts = Build(points);
        var fromZero = Build(points, beginAtZero: true);

        Assert.Equal((200, 10, 5, 1), charts.TickSteps);
        Assert.Equal((500, 20, 5, 1), fromZero.TickSteps);
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void MarkersAreShownOnlyWhileThereIsRoomForThem(int readings, bool shown)
    {
        var charts = Build(Enumerable.Range(0, readings).Select(hour => Reading(hour, download: 100)).ToList());

        Assert.Equal(shown, charts.ShowMarkers);
    }

    [Fact]
    public void NoReadingsGiveEmptyCharts()
    {
        var charts = Build([Failure(0, "timeout")]);

        Assert.Empty(charts.Labels);
        Assert.Empty(charts.Download[1].Data.Values);
        Assert.Single(charts.Failures);
    }

    private static DashboardCharts Build(IReadOnlyList<ChartPoint> points, Func<double, double>? convertSpeed = null, bool beginAtZero = false) =>
        DashboardCharts.Build(points, convertSpeed ?? (mbps => mbps), time => time.ToString("HH:mm", CultureInfo.InvariantCulture), beginAtZero);

    private static ChartPoint Reading(int hour, double? download, double upload = 10, int ping = 10, double jitter = 1) =>
        new(_start.AddHours(hour), Failed: false, Error: null, ping, jitter, download, upload, BufferbloatDown: null, BufferbloatUp: null, Duration: 20);

    private static ChartPoint Failure(int hour, string error) =>
        new(_start.AddHours(hour), Failed: true, error, null, null, null, null, null, null, null);
}
