using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.UnitTests.Application.Providers;

public class CloudflareToolTests
{
    private static readonly RunOptions _automatic = new(null, null, null, "unused.json");

    private readonly CloudflareTool _tool = new();

    [Fact]
    public void TheBestSpeedsAndAverageLatencyAreReported()
    {
        const string Output = """{"latency_measurement":{"avg_latency_ms":14.8,"latency_measurements":[14.0,15.2,14.5,16.0]},"speed_measurements":[{"test_type":"Download","max":450.2,"median":420.0},{"test_type":"Upload","max":95.8,"median":90.0}],"elapsed":25000}""";

        var result = _tool.ParseResult(new ToolOutput(Output, "", 0), _automatic);

        Assert.True(result.Success);
        Assert.Equal((15, 450.2, 95.8, 25), (result.Ping, result.Download, result.Upload, result.Time));
        Assert.NotNull(result.Jitter);
    }

    [Fact]
    public void TheBytesMovedAreWorkedOutFromThePayloadSizes()
    {
        const string Run = """{"metadata":{"country":"GB","ip":"203.0.113.9","colo":"LHR"},"latency_measurement":{"avg_latency_ms":78.8,"latency_measurements":[53.2]},"speed_measurements":[{"test_type":"Download","payload_size":100000,"median":13.1,"max":13.1,"successes":3},{"test_type":"Upload","payload_size":100000,"median":9.4,"max":9.4,"successes":2}]}""";

        var result = _tool.ParseResult(new ToolOutput(Run, "", 0), _automatic);

        Assert.Equal((300000L, 200000L), (result.DownloadBytes, result.UploadBytes));
        Assert.Null(result.PacketLoss);
    }

    [Theory]
    [InlineData("192.168.1.20", "--ipv4=192.168.1.20")]
    [InlineData("2a00:23c8:870c:bf00::1", "--ipv6=2a00:23c8:870c:bf00::1")]
    public void TheNetworkInterfacePicksTheAddressFamily(string address, string expected)
    {
        Assert.Equal(["--output-format=json", expected], _tool.BuildArguments(new RunOptions(null, null, address, "unused.json")).Arguments);
    }

    [Fact]
    public void CloudflareHasNoServerChoice()
    {
        Assert.Null(_tool.Servers);
    }

    [Fact]
    public void ACloudflareItCannotReachGivesAClearError()
    {
        var output = new ToolOutput("", "Error fetching metadata: error sending request for url (https://speed.cloudflare.com/cdn-cgi/trace)", 1);

        Assert.Equal("Cloudflare couldn't reach speed.cloudflare.com. Check the internet connection.", _tool.ParseResult(output, _automatic).Error);
        Assert.Equal("Cloudflare couldn't reach speed.cloudflare.com through the network interface 10.255.255.1.",
            _tool.ParseResult(output, new RunOptions(null, null, "10.255.255.1", "unused.json")).Error);
    }

    [Fact]
    public void RateLimitingSaysToTryAgainLater()
    {
        var result = _tool.ParseResult(new ToolOutput("", "Error: 429 Too Many Requests", 1), _automatic);

        Assert.Equal("Cloudflare is limiting how often tests can run. Try again later.", result.Error);
    }
}
