using SpeedtestWatcher.Web.Helpers;

namespace SpeedtestWatcher.Tests;

public class SpeedtestErrorsTests
{
    [Theory]
    [InlineData("[error] Network unreachable (101)", "Internet connection was unstable during the time of the test")]
    [InlineData("Timeout occurred in connect.", "The test took too long and was canceled")]
    [InlineData("spawn speedtest permission denied", "Speedtest Watcher has no permission to start this test")]
    [InlineData("socket: Connection refused", "The test could not be performed because the connection was rejected")]
    [InlineData("request timed out", "Internet connection was unstable during the time of the test")]
    public void Describe_MapsKnownProviderErrors(string raw, string expected)
    {
        Assert.Equal(expected, SpeedtestErrors.Describe(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Describe_MissingErrorIsUnknown(string? raw)
    {
        Assert.Equal("Unknown error", SpeedtestErrors.Describe(raw));
    }

    [Fact]
    public void Describe_UnrecognisedErrorKeepsTheRawText()
    {
        Assert.Equal("Unknown error: disk full", SpeedtestErrors.Describe("disk full"));
    }
}
