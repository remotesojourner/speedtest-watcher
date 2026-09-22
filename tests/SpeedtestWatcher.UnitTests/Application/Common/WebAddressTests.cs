using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.UnitTests.Application.Common;

public sealed class WebAddressTests
{
    [Theory]
    [InlineData("https://auth.example.com/application/o/speedtest-watcher/", true)]
    [InlineData("http://192.168.1.10:8080", true)]
    [InlineData("  https://icanhazip.com  ", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("/relative/path", false)]
    [InlineData("example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyAbsoluteHttpAndHttpsAddressesAreAccepted(string? value, bool expected)
    {
        Assert.Equal(expected, WebAddress.IsHttp(value));
    }
}
