using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public class SpeedtestErrorsTests
{
    [Theory]
    [InlineData("[error] Network unreachable (101)", "Internet connection was unstable during the time of the test")]
    [InlineData("Timeout occurred in connect.", "The test took too long and was canceled")]
    [InlineData("spawn speedtest permission denied", "Speedtest Watcher has no permission to start this test")]
    [InlineData("socket: Connection refused", "The test could not be performed because the connection was rejected")]
    [InlineData("request timed out", "Internet connection was unstable during the time of the test")]
    [InlineData("Configuration - Could not retrieve or read configuration (ConfigurationError)", "Ookla couldn't download its test configuration from speedtest.net")]
    public void DescribeMapsKnownProviderErrors(string raw, string expected)
    {
        Assert.Equal(expected, SpeedtestErrors.Describe(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DescribeMissingErrorIsUnknown(string? raw)
    {
        Assert.Equal("Unknown error", SpeedtestErrors.Describe(raw));
    }

    [Fact]
    public void DescribeShowsAnyOtherErrorAsItIs()
    {
        Assert.Equal("LibreSpeed has no server 7032. Pick one from its server list.", SpeedtestErrors.Describe("LibreSpeed has no server 7032. Pick one from its server list."));
    }
}
