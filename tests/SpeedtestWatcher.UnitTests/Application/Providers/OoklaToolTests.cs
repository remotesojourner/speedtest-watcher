using System.Runtime.InteropServices;
using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.UnitTests.Application.Providers;

public class OoklaToolTests
{
    private const string Result = """{"type":"result","timestamp":"2026-09-14T00:00:00Z","ping":{"jitter":1.45,"latency":12.3},"download":{"bandwidth":125000000,"bytes":125000000,"elapsed":10000},"upload":{"bandwidth":62500000,"bytes":62500000,"elapsed":10000},"server":{"id":1234,"name":"Vodafone","host":"speedtest.vodafone.de"},"result":{"id":"abc-123","url":"https://www.speedtest.net/result/abc-123"}}""";

    private readonly OoklaTool _tool = new();

    [Fact]
    public void TheResultLineIsParsedAndProgressLinesAreIgnored()
    {
        var output = string.Join('\n', """{"type":"testStart","isp":"Acme"}""", Result, """{"type":"log","message":"done"}""");

        var result = _tool.ParseResult(output)!;

        Assert.True(result.Success);
        Assert.Equal((12, 1.45, 1000.0, 500.0, 20), (result.Ping, result.Jitter!.Value, result.Download, result.Upload, result.Time));
        Assert.Equal((1234, "Vodafone", "speedtest.vodafone.de", "abc-123"), (result.ServerId, result.ServerName, result.ServerHost, result.ResultId));
    }

    [Fact]
    public void OutputWithoutAResultLineHasNoResult()
    {
        Assert.Null(_tool.ParseResult("""{"type":"testStart"}""" + "\nnot json"));
    }

    [Fact]
    public void TheChosenServerIsPassedToTheCli()
    {
        var arguments = _tool.BuildArguments(new RunOptions("4242", null, null, "unused.json"));

        Assert.Equal(["--accept-license", "--accept-gdpr", "--format=json", "--server-id=4242"], arguments.Arguments);
        Assert.Null(arguments.ScratchFileContent);
    }

    [Fact]
    public void DownloadsExistOnlyForSupportedPlatforms()
    {
        Assert.EndsWith("linux-aarch64.tgz", _tool.DownloadUrl(new PlatformTarget(OSPlatform.Linux, Architecture.Arm64)));
        Assert.Null(_tool.DownloadUrl(new PlatformTarget(OSPlatform.OSX, Architecture.Arm64)));
    }

    [Fact]
    public void ServerListsAreReadIntoTypedServers()
    {
        var servers = _tool.Servers!.Parse("""[{"id":"12345","name":"London","sponsor":"Acme Fibre","country":"United Kingdom","distance":4.2,"host":"speed.acme.example:8080"}]""");

        Assert.Equal([new ServerInfo("12345", "London", "Acme Fibre", "United Kingdom", 4.2, "speed.acme.example:8080")], servers);
    }
}
