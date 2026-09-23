using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.UnitTests.Application.Common;

public class DurationTests
{
    [Theory]
    [InlineData(0, "0 seconds")]
    [InlineData(1, "1 second")]
    [InlineData(59, "59 seconds")]
    [InlineData(60, "1 minute")]
    [InlineData(90, "1 minute 30 seconds")]
    [InlineData(3600, "1 hour")]
    [InlineData(5400, "1 hour 30 minutes")]
    [InlineData(86400, "1 day")]
    [InlineData(180000, "2 days 2 hours")]
    public void LengthsReadInTheTwoLargestUnitsThatFit(int seconds, string expected)
    {
        Assert.Equal(expected, Duration.Describe(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void ALengthBelowZeroReadsAsNothing()
    {
        Assert.Equal("0 seconds", Duration.Describe(TimeSpan.FromSeconds(-30)));
    }
}
