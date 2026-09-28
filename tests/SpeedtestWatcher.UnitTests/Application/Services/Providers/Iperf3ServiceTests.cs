using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Services.Providers;

namespace SpeedtestWatcher.UnitTests.Application.Services.Providers;

public sealed class Iperf3ServiceTests : IDisposable
{
    private const string DownloadResult = """
        {"start":{"connected":[{"socket":5,"local_host":"127.0.0.1","local_port":2282,"remote_host":"127.0.0.1","remote_port":15201}],"version":"iperf 3.21","test_start":{"protocol":"TCP","reverse":1,"duration":2}},"intervals":[],"end":{"sum_sent":{"start":0,"end":2.010326,"seconds":2.010326,"bytes":9919004672,"bits_per_second":39472223597.565765},"sum_received":{"start":0,"end":2.009817,"seconds":2.009817,"bytes":9918480384,"bits_per_second":39480133301.68866}}}
        """;

    private const string UploadResult = """
        {"start":{"connected":[{"socket":5,"local_host":"127.0.0.1","local_port":2288,"remote_host":"127.0.0.1","remote_port":15201}],"version":"iperf 3.21","test_start":{"protocol":"TCP","reverse":0,"duration":2}},"intervals":[],"end":{"sum_sent":{"start":0,"end":2.01554,"seconds":2.01554,"bytes":9300869120,"bits_per_second":36916634232.017227},"sum_received":{"start":0,"end":2.016323,"seconds":2.016323,"bytes":9300869120,"bits_per_second":36902298371.838242}}}
        """;

    private const string UdpResult = """
        {"start":{"version":"iperf 3.21","test_start":{"protocol":"UDP","reverse":1,"duration":2}},"intervals":[],"end":{"sum_sent":{"start":0,"end":2.003259,"seconds":2.003259,"bytes":261980,"bits_per_second":1046215.1923440754,"jitter_ms":0,"lost_packets":0,"packets":4,"lost_percent":0},"sum_received":{"start":0,"end":2.002466,"seconds":2.002466,"bytes":261980,"bits_per_second":1046629.5058193248,"jitter_ms":0.028753173828125,"lost_packets":1,"packets":4,"lost_percent":25}}}
        """;

    private const string UdpNothingArrived = """
        {"start":{"version":"iperf 3.21","test_start":{"protocol":"UDP","reverse":1,"duration":5}},"intervals":[],"end":{"sum_received":{"start":0,"end":5.0,"seconds":5.0,"bytes":0,"bits_per_second":0,"jitter_ms":0,"lost_packets":0,"packets":0,"lost_percent":0}}}
        """;

    private const string RefusedError = """
        {"start":{"connected":[],"version":"iperf 3.21"},"intervals":[],"end":{},"error":"unable to connect to server - server may have stopped running or use a different port, firewall issue, etc.: Connection refused"}
        """;

    private const string BusyError = """
        {"start":{"connected":[],"version":"iperf 3.21"},"intervals":[],"end":{},"error":"the server is busy running a test. try again later"}
        """;

    private const string TimedOutError = """
        {"start":{"connected":[],"version":"iperf 3.21"},"intervals":[],"end":{},"error":"unable to connect to server - server may have stopped running or use a different port, firewall issue, etc.: Connection timed out"}
        """;

    private static readonly RunOptions _server = new(null, "192.168.1.10:5201", null, "unused.json");

    private readonly Iperf3Service _tool = new(NullLogger<Iperf3Service>.Instance);
    private readonly ICliProcessService _processes = A.Fake<ICliProcessService>();
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

    public Iperf3ServiceTests()
    {
        _listener.Start();
    }

    private RunOptions Listening => new(null, $"127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}", null, "unused.json");

    [Fact]
    public void TheDownloadRunReportsSpeedBytesAndTime()
    {
        var result = _tool.ParseResult(new ToolOutput(DownloadResult, "", 0), _server);

        Assert.True(result.Success);
        Assert.Equal((39480.13, 9918480384L, 2), (result.Download, result.DownloadBytes!.Value, result.Time));
        Assert.Equal(("192.168.1.10:5201", "192.168.1.10"), (result.ServerName, result.ServerHost));
    }

    [Theory]
    [InlineData(RefusedError, "The iperf3 server at 192.168.1.10:5201 refused the connection. Check that iperf3 -s is running there.")]
    [InlineData(BusyError, "The iperf3 server at 192.168.1.10:5201 is busy running another test. Try again shortly.")]
    [InlineData(TimedOutError, "The iperf3 server at 192.168.1.10:5201 couldn't be reached. Check the host, port and firewall.")]
    public void KnownErrorsAreExplained(string output, string expected)
    {
        var result = _tool.ParseResult(new ToolOutput(output, "", 1), _server);

        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
    }

    [Fact]
    public void AnUnknownErrorIsQuoted()
    {
        const string Output = """{"start":{},"intervals":[],"end":{},"error":"parameter error - must either be a client (-c) or server (-s)"}""";

        Assert.Equal("iperf3 stopped with an error: parameter error - must either be a client (-c) or server (-s)",
            _tool.ParseResult(new ToolOutput(Output, "", 1), _server).Error);
    }

    [Fact]
    public void OutputThatIsNotJsonIsNotASuccess()
    {
        Assert.Equal("iperf3 stopped with an error: not json", _tool.ParseResult(new ToolOutput("not json", "", 1), _server).Error);
    }

    [Fact]
    public void TheServerAndInterfaceArePassedToTheCli()
    {
        var arguments = _tool.BuildArguments(new RunOptions(null, "192.168.1.10:5201", "10.0.0.5", "unused.json"));

        Assert.Equal(["-c", "192.168.1.10", "-p", "5201", "-J", "--connect-timeout", "5000", "--bind=10.0.0.5", "-R"], arguments.Arguments);
    }

    [Fact]
    public void AnIpv6ServerIsPassedWithoutBrackets()
    {
        var arguments = _tool.BuildArguments(new RunOptions(null, "[2001:db8::5]:5201", null, "unused.json"));

        Assert.Equal(["-c", "2001:db8::5", "-p", "5201", "-J", "--connect-timeout", "5000", "-R"], arguments.Arguments);
    }

    [Fact]
    public void Iperf3HasNoServerCatalogAndIsNeverDownloaded()
    {
        Assert.Null(_tool.Servers);
        Assert.Null(_tool.DownloadUrl(new PlatformTarget(OSPlatform.Windows, Architecture.X64)));
        Assert.Null(_tool.DownloadUrl(new PlatformTarget(OSPlatform.Linux, Architecture.Arm64)));
    }

    [Fact]
    public async Task WithNoServerTheRunFailsWithoutStartingIperf3()
    {
        var result = await _tool.RunAsync(_processes, "iperf3", new RunOptions(null, null, null, "unused.json"), TestContext.Current.CancellationToken);

        Assert.Equal("No iperf3 servers are configured. Add one on the Provider tab.", result.Error);
        A.CallTo(_processes).MustNotHaveHappened();
    }

    [Fact]
    public async Task ARefusedServerIsReportedByIperf3WithoutPinging()
    {
        Answer(new ProcessOutcome(new ToolOutput(RefusedError, "", 1), null));

        var result = await _tool.RunAsync(_processes, "iperf3", _server, TestContext.Current.CancellationToken);

        Assert.Equal("The iperf3 server at 192.168.1.10:5201 refused the connection. Check that iperf3 -s is running there.", result.Error);
    }

    [Fact]
    public async Task APingWithNoAnswerAfterTheRunsFailsTheTest()
    {
        var options = Listening;
        _listener.Stop();
        Answer(Output(DownloadResult), Output(UploadResult), Output(UdpResult));

        var result = await _tool.RunAsync(_processes, "iperf3", options, TestContext.Current.CancellationToken);

        Assert.Equal($"The iperf3 server at {options.CustomServerUrl} couldn't be reached. Check the host, port and firewall.", result.Error);
    }

    [Fact]
    public async Task ADownloadFailureStopsBeforeTheUpload()
    {
        Answer(new ProcessOutcome(new ToolOutput(BusyError, "", 1), null));

        var result = await _tool.RunAsync(_processes, "iperf3", Listening, TestContext.Current.CancellationToken);

        Assert.StartsWith("The iperf3 server at 127.0.0.1:", result.Error);
        Assert.EndsWith("is busy running another test. Try again shortly.", result.Error);
        A.CallTo(_processes).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AMissingIperf3IsReportedAsIs()
    {
        Answer(new ProcessOutcome(null, "The iperf3 command-line tool isn't installed. Install it and make sure it's on the PATH."));

        var result = await _tool.RunAsync(_processes, "iperf3", Listening, TestContext.Current.CancellationToken);

        Assert.Equal("The iperf3 command-line tool isn't installed. Install it and make sure it's on the PATH.", result.Error);
    }

    [Fact]
    public async Task ASuccessfulRunCombinesPingDownloadUploadAndUdp()
    {
        Answer(Output(DownloadResult), Output(UploadResult), Output(UdpResult));

        var result = await _tool.RunAsync(_processes, "iperf3", Listening, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.InRange(result.Ping, 0, 2000);
        Assert.Equal((39480.13, 36902.3, 4), (result.Download, result.Upload, result.Time));
        Assert.Equal((9918480384L, 9300869120L), (result.DownloadBytes!.Value, result.UploadBytes!.Value));
        Assert.Equal((0.03, 25.0), (result.Jitter!.Value, result.PacketLoss!.Value));
    }

    [Fact]
    public async Task TheRunsAreDownloadThenUploadThenAShortUdpRun()
    {
        var calls = new List<IReadOnlyList<string>>();
        A.CallTo(() => _processes.RunAsync(A<string>._, A<string>._, A<IReadOnlyList<string>>._, A<TimeSpan?>._, A<CancellationToken>._))
            .Invokes((string _, string _, IReadOnlyList<string> arguments, TimeSpan? _, CancellationToken _) => calls.Add(arguments))
            .ReturnsNextFromSequence(Output(DownloadResult), Output(UploadResult), Output(UdpResult));

        await _tool.RunAsync(_processes, "iperf3", Listening, TestContext.Current.CancellationToken);

        Assert.Equal(3, calls.Count);
        Assert.Contains("-R", calls[0]);
        Assert.DoesNotContain("-R", calls[1]);
        Assert.DoesNotContain("-u", calls[1]);
        Assert.Contains("-u", calls[2]);
        Assert.Contains("-R", calls[2]);
    }

    [Theory]
    [InlineData(UdpNothingArrived)]
    [InlineData(TimedOutError)]
    public async Task AUdpRunWithoutPacketsStillStoresSpeedsAndPing(string udpOutput)
    {
        Answer(Output(DownloadResult), Output(UploadResult), Output(udpOutput));

        var result = await _tool.RunAsync(_processes, "iperf3", Listening, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal((39480.13, 36902.3), (result.Download, result.Upload));
        Assert.Null(result.Jitter);
        Assert.Null(result.PacketLoss);
    }

    [Fact]
    public async Task AUdpRunThatTimesOutStillStoresSpeedsAndPing()
    {
        Answer(Output(DownloadResult), Output(UploadResult), new ProcessOutcome(null, "Speedtest timed out after 30 seconds"));

        var result = await _tool.RunAsync(_processes, "iperf3", Listening, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Null(result.PacketLoss);
    }

    public void Dispose() => _listener.Dispose();

    private static ProcessOutcome Output(string json) => new(new ToolOutput(json, "", 0), null);

    private void Answer(params ProcessOutcome[] outcomes) =>
        A.CallTo(() => _processes.RunAsync(A<string>._, A<string>._, A<IReadOnlyList<string>>._, A<TimeSpan?>._, A<CancellationToken>._))
            .ReturnsNextFromSequence(outcomes);
}
