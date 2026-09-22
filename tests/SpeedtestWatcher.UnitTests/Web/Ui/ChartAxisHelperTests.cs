using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public class ChartAxisHelperTests
{
    private static readonly string[] TenLabels = Enumerable.Range(0, 10).Select(i => $"L{i}").ToArray();

    [Fact]
    public void ThinLabels_KeepsFirstLastAndAtMostMax()
    {
        var thinned = ChartAxisHelper.ThinLabels(TenLabels, 3);

        Assert.Equal(TenLabels.Length, thinned.Length);
        Assert.Equal(3, thinned.Count(label => label != ""));
        Assert.Equal("L0", thinned[0]);
        Assert.Equal("L9", thinned[^1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(20)]
    public void ThinLabels_ReturnsEveryLabelWhenNoThinningNeeded(int max)
    {
        Assert.Equal(TenLabels, ChartAxisHelper.ThinLabels(TenLabels, max));
    }

    [Fact]
    public void ThinLabels_MaxOfOneStillKeepsBothEnds()
    {
        var thinned = ChartAxisHelper.ThinLabels(TenLabels, 1);

        Assert.Equal("L0", thinned[0]);
        Assert.Equal("L9", thinned[^1]);
    }

    [Theory]
    [InlineData(new[] { 940.3, 941.6 }, 1)]
    [InlineData(new double[] { 0, 14 }, 5)]
    [InlineData(new double[] { 100, 180 }, 20)]
    [InlineData(new double[] { 0, 960 }, 500)]
    [InlineData(new double[] { 42 }, 1)]
    [InlineData(new double[0], 1)]
    public void TickStep_SplitsRangeIntoNiceSteps(double[] values, int expected)
    {
        Assert.Equal(expected, ChartAxisHelper.TickStep(values));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(24, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    [InlineData(168, false)]
    public void HasRoomForMarkers_OnlyWhilePointsStayApart(int pointCount, bool expected)
    {
        Assert.Equal(expected, ChartAxisHelper.HasRoomForMarkers(pointCount));
    }

    [Theory]
    [InlineData(new[] { 940.3, 961.6 })]
    [InlineData(new[] { 108.2, 112.4 })]
    [InlineData(new[] { 11.0, 14.0 })]
    public void TickStep_WhenTheAxisBeginsAtZero_KeepsTheWholeAxisToAFewTicks(double[] values)
    {
        var step = ChartAxisHelper.TickStep(values, beginAtZero: true);

        var ticksFromZeroToTop = (int)Math.Ceiling(values.Max() / step) + 1;
        Assert.InRange(ticksFromZeroToTop, 3, 6);
    }
}
