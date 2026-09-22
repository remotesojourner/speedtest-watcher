using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.UnitTests.Application.Providers;

public class CloudflareToolTests
{
    private readonly CloudflareTool _tool = new();

    [Fact]
    public void TheBestSpeedsAndAverageLatency_AreReported()
    {
        var result = _tool.ParseResult("""{"latency_measurement":{"avg_latency_ms":14.8,"latency_measurements":[14.0,15.2,14.5,16.0]},"speed_measurements":[{"test_type":"Download","max":450.2,"median":420.0},{"test_type":"Upload","max":95.8,"median":90.0}],"elapsed":25000}""")!;

        Assert.True(result.Success);
        Assert.Equal((15, 450.2, 95.8, 25), (result.Ping, result.Download, result.Upload, result.Time));
        Assert.NotNull(result.Jitter);
    }

    [Theory]
    [InlineData("192.168.1.20", "--ipv4=192.168.1.20")]
    [InlineData("2a00:23c8:870c:bf00::1", "--ipv6=2a00:23c8:870c:bf00::1")]
    public void TheNetworkInterface_PicksTheAddressFamily(string address, string expected)
    {
        Assert.Equal(["--output-format=json", expected], _tool.BuildArguments(new RunOptions(null, null, address, "unused.json")).Arguments);
    }

    [Fact]
    public void Cloudflare_HasNoServerChoice()
    {
        Assert.Null(_tool.Servers);
    }
}
