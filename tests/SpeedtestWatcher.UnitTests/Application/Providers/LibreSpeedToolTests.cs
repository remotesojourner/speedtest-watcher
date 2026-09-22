using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.UnitTests.Application.Providers;

public class LibreSpeedToolTests
{
    private static readonly RunOptions _automatic = new(null, null, null, "unused.json");

    private readonly LibreSpeedTool _tool = new();

    [Fact]
    public void AResultWrappedInAnArrayIsParsed()
    {
        var result = _tool.ParseResult(new ToolOutput("""[{"ping":18.4,"jitter":"2.35","download":240.5,"upload":45.2,"elapsed":12000,"server":{"id":5,"name":"Local Libre","url":"http://speed.local"}}]""", "", 0), _automatic);

        Assert.True(result.Success);
        Assert.Equal((18, 2.35, 240.5, 45.2, 12), (result.Ping, result.Jitter!.Value, result.Download, result.Upload, result.Time));
        Assert.Equal((5, "Local Libre", "http://speed.local"), (result.ServerId, result.ServerName, result.ServerHost));
    }

    [Fact]
    public void ACustomServerIsWrittenToTheScratchFileAndUsedInsteadOfTheServerId()
    {
        var arguments = _tool.BuildArguments(new RunOptions("7", "https://speed.example/backend/", "192.168.1.20", "custom.json"));

        Assert.Equal(["--json", "--duration=5", "--no-icmp", "--source=192.168.1.20", "--local-json=custom.json", "--server=1"], arguments.Arguments);
        Assert.Contains("https://speed.example/backend/", arguments.ScratchFileContent);
    }

    [Fact]
    public void AChosenServerIsPassedToTheCli()
    {
        Assert.Equal(["--json", "--duration=5", "--no-icmp", "--server=7"], _tool.BuildArguments(new RunOptions("7", null, null, "unused.json")).Arguments);
    }

    [Fact]
    public void TheBytesMovedAreRecorded()
    {
        const string Run = """[{"ping":18.4,"jitter":"2.35","download":240.5,"upload":45.2,"elapsed":12000,"bytes_sent":74481664,"bytes_received":266796304}]""";

        var result = _tool.ParseResult(new ToolOutput(Run, "", 0), _automatic);

        Assert.Equal((266796304L, 74481664L), (result.DownloadBytes, result.UploadBytes));
        Assert.Null(result.PacketLoss);
    }

    [Fact]
    public void AServerLibreSpeedDoesNotHaveIsNamedInTheError()
    {
        var result = _tool.ParseResult(new ToolOutput("null\n", "", 0), new RunOptions("7032", null, null, "unused.json"));

        Assert.False(result.Success);
        Assert.Equal("LibreSpeed has no server 7032. Pick one from its server list.", result.Error);
    }

    [Fact]
    public void AServerListThatCannotBeDownloadedGivesTheReason()
    {
        const string Errors = """
            Error when fetching server list: Get "https://librespeed.org/backend-servers/servers.php/.well-known/librespeed": dial tcp 10.255.255.1:0->78.46.162.45:443: bind: The requested address is not valid in its context.
            Terminated due to error
            """;

        var result = _tool.ParseResult(new ToolOutput("", Errors, 1), new RunOptions(null, null, "10.255.255.1", "unused.json"));

        Assert.Equal("LibreSpeed couldn't download its server list through the network interface 10.255.255.1: The requested address is not valid in its context.", result.Error);
    }

    [Fact]
    public void AnythingElseQuotesTheFirstErrorLine()
    {
        var result = _tool.ParseResult(new ToolOutput("", "Error when pinging server: context deadline exceeded\nTerminated due to error", 1), _automatic);

        Assert.Equal("LibreSpeed stopped with an error: Error when pinging server: context deadline exceeded", result.Error);
    }
}
