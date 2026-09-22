using System.Runtime.InteropServices;
using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.UnitTests.Application.Providers;

public class OoklaToolTests
{
    private const string Result = """{"type":"result","timestamp":"2026-09-14T00:00:00Z","ping":{"jitter":1.45,"latency":12.3},"download":{"bandwidth":125000000,"bytes":125000000,"elapsed":10000},"upload":{"bandwidth":62500000,"bytes":62500000,"elapsed":10000},"server":{"id":1234,"name":"Vodafone","host":"speedtest.vodafone.de"},"result":{"id":"abc-123","url":"https://www.speedtest.net/result/abc-123"}}""";

    private const string CannotBind = """
        [2026-09-22 15:20:20.484] [error] Configuration - Failed binding local connection end (UnknownException)
        [2026-09-22 15:20:20.485] [error] Configuration - Cannot retrieve configuration document (0)
        [2026-09-22 15:20:20.485] [error] ConfigurationError - Could not retrieve or read configuration (Configuration)
        {"type":"log","timestamp":"2026-09-22T14:20:20Z","message":"Configuration - Could not retrieve or read configuration (ConfigurationError)","level":"error"}
        """;

    private static readonly RunOptions _automatic = new(null, null, null, "unused.json");

    private readonly OoklaTool _tool = new();

    [Fact]
    public void TheResultLineIsParsedAndProgressLinesAreIgnored()
    {
        var output = string.Join('\n', """{"type":"testStart","isp":"Acme"}""", Result, """{"type":"log","message":"done"}""");

        var result = _tool.ParseResult(new ToolOutput(output, "", 0), _automatic);

        Assert.True(result.Success);
        Assert.Equal((12, 1.45, 1000.0, 500.0, 20), (result.Ping, result.Jitter!.Value, result.Download, result.Upload, result.Time));
        Assert.Equal((1234, "Vodafone", "speedtest.vodafone.de", "abc-123"), (result.ServerId, result.ServerName, result.ServerHost, result.ResultId));
    }

    [Fact]
    public void OutputWithoutAResultLineIsNotASuccess()
    {
        Assert.False(_tool.ParseResult(new ToolOutput("""{"type":"testStart"}""" + "\nnot json", "", 0), _automatic).Success);
    }

    [Fact]
    public void AServerOoklaDoesNotHaveIsNamedInTheError()
    {
        const string Errors = """{"type":"log","timestamp":"2026-09-22T14:20:20Z","message":"Configuration - No servers defined (NoServersException)","level":"error"}""";

        var result = _tool.ParseResult(new ToolOutput("", Errors, 2), new RunOptions("999999999", null, null, "unused.json"));

        Assert.False(result.Success);
        Assert.Equal("Ookla has no server 999999999. Pick one from its server list, or check the ID on speedtest.net.", result.Error);
    }

    [Fact]
    public void ANetworkInterfaceOoklaCannotUseIsNamedInTheError()
    {
        var result = _tool.ParseResult(new ToolOutput("", CannotBind, 2), new RunOptions(null, null, "10.255.255.1", "unused.json"));

        Assert.Equal("Ookla couldn't send traffic through the network interface 10.255.255.1.", result.Error);
    }

    [Fact]
    public void AnyOtherErrorGivesTheFirstOneOoklaLogged()
    {
        var result = _tool.ParseResult(new ToolOutput("", CannotBind, 2), _automatic);

        Assert.Equal("Ookla couldn't run the test: Failed binding local connection end.", result.Error);
    }

    [Fact]
    public void NoOutputAtAllGivesTheExitCode()
    {
        Assert.Equal("Ookla stopped without a result (exit code 3).", _tool.ParseResult(new ToolOutput("", "", 3), _automatic).Error);
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
