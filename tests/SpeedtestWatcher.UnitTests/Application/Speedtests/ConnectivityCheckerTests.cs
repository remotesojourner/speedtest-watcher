using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.UnitTests.Application.Speedtests;

public sealed class ConnectivityCheckerTests : IDisposable
{
    private readonly RecordingHandler _handler = new();
    private readonly ConnectivityChecker _checker;

    public ConnectivityCheckerTests()
    {
        _checker = new ConnectivityChecker(new StubHttpClientFactory(_handler), NullLogger<ConnectivityChecker>.Instance);
    }

    [Fact]
    public async Task ThePublicIpComesBackWithAPassedCheck()
    {
        _handler.ResponseBody = "203.0.113.9\n";

        var check = await _checker.CheckAsync(Settings([]), TestContext.Current.CancellationToken);

        Assert.Equal((true, null, "203.0.113.9"), (check.Proceed, check.SkipReason, check.PublicIp));
    }

    [Fact]
    public async Task ASkippedTestStillKnowsThePublicIp()
    {
        _handler.ResponseBody = "203.0.113.9";

        var check = await _checker.CheckAsync(Settings(["203.0.113.9"]), TestContext.Current.CancellationToken);

        Assert.Equal((false, "Public IP 203.0.113.9 is on the skip list", "203.0.113.9"), (check.Proceed, check.SkipReason, check.PublicIp));
    }

    [Fact]
    public async Task ACheckThatIsTurnedOffLooksUpNothing()
    {
        var check = await _checker.CheckAsync(new PreTestCheckSettings(false, "https://localhost/ip", []), TestContext.Current.CancellationToken);

        Assert.Equal((true, null), (check.Proceed, check.PublicIp));
        Assert.Empty(_handler.Requests);
    }

    private static PreTestCheckSettings Settings(IReadOnlyList<string> skipIps) => new(true, "https://localhost/ip", skipIps);

    public void Dispose() => _handler.Dispose();
}
