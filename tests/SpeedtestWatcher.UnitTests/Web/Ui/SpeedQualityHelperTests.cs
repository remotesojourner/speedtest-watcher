using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public class SpeedQualityHelperTests
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
    public void GetQualityComparesAReadingWithItsTarget(double current, double optimal, bool higherIsBetter, SpeedQuality expected)
    {
        var quality = SpeedQualityHelper.GetQuality(current, optimal, higherIsBetter);
        Assert.Equal(expected, quality);
    }
}
