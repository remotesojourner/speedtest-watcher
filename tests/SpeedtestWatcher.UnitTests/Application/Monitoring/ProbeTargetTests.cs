using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.UnitTests.Application.Monitoring;

public class ProbeTargetTests
{
    [Theory]
    [InlineData("1.1.1.1:443", "1.1.1.1", 443)]
    [InlineData(" example.com:8080 ", "example.com", 8080)]
    [InlineData("[2606:4700:4700::1111]:443", "2606:4700:4700::1111", 443)]
    public void AHostAndPortAreRead(string value, string host, int port)
    {
        Assert.Equal(new ProbeTarget(host, port), ProbeTarget.Parse(value));
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("1.1.1.1:")]
    [InlineData(":443")]
    [InlineData("1.1.1.1:0")]
    [InlineData("1.1.1.1:70000")]
    [InlineData("1.1.1.1:https")]
    [InlineData("two words:443")]
    [InlineData("")]
    public void AnythingElseIsRefused(string value)
    {
        Assert.Null(ProbeTarget.Parse(value));
    }

    [Fact]
    public void AListKeepsOnlyTheTargetsThatMakeSense()
    {
        var targets = ProbeTarget.ParseList(["1.1.1.1:443", "nonsense", "8.8.8.8:53"]);

        Assert.Equal(["1.1.1.1:443", "8.8.8.8:53"], targets.Select(target => target.ToString()));
    }

    [Fact]
    public void AnIpv6TargetIsWrittenBackInBrackets()
    {
        Assert.Equal("[2606:4700:4700::1111]:443", new ProbeTarget("2606:4700:4700::1111", 443).ToString());
    }
}
