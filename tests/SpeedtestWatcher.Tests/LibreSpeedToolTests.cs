using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.Tests;

public class LibreSpeedToolTests
{
    private readonly LibreSpeedTool _tool = new();

    [Fact]
    public void AResultWrappedInAnArray_IsParsed()
    {
        var result = _tool.ParseResult("""[{"ping":18.4,"jitter":"2.35","download":240.5,"upload":45.2,"elapsed":12000,"server":{"id":5,"name":"Local Libre","url":"http://speed.local"}}]""")!;

        Assert.True(result.Success);
        Assert.Equal((18, 2.35, 240.5, 45.2, 12), (result.Ping, result.Jitter!.Value, result.Download, result.Upload, result.Time));
        Assert.Equal((5, "Local Libre", "http://speed.local"), (result.ServerId, result.ServerName, result.ServerHost));
    }

    [Fact]
    public void ACustomServer_IsWrittenToTheScratchFile_AndUsedInsteadOfTheServerId()
    {
        var arguments = _tool.BuildArguments(new RunOptions("7", "https://speed.example/backend/", "192.168.1.20", "custom.json"));

        Assert.Equal(["--json", "--duration=5", "--source=192.168.1.20", "--local-json=custom.json", "--server=1"], arguments.Arguments);
        Assert.Contains("https://speed.example/backend/", arguments.ScratchFileContent);
    }

    [Fact]
    public void AChosenServer_IsPassedToTheCli()
    {
        Assert.Equal(["--json", "--duration=5", "--server=7"], _tool.BuildArguments(new RunOptions("7", null, null, "unused.json")).Arguments);
    }
}
