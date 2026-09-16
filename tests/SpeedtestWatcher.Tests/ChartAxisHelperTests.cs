using SpeedtestWatcher.Web.Helpers;

namespace SpeedtestWatcher.Tests;

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
    [InlineData(new double[] { 940.3, 941.6 }, 1)]
    [InlineData(new double[] { 0, 14 }, 5)]
    [InlineData(new double[] { 100, 180 }, 20)]
    [InlineData(new double[] { 0, 960 }, 500)]
    [InlineData(new double[] { 42 }, 1)]
    [InlineData(new double[0], 1)]
    public void TickStep_SplitsRangeIntoNiceSteps(double[] values, int expected)
    {
        Assert.Equal(expected, ChartAxisHelper.TickStep(values));
    }
}
