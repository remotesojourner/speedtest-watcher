using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Services;

namespace SpeedtestWatcher.UnitTests.Application.Services;

public sealed class ConnectivityCheckServiceTests : IDisposable
{
    private readonly RecordingHandler _handler = new();
    private readonly ConnectivityCheckService _connectivity;

    public ConnectivityCheckServiceTests()
    {
        _connectivity = new ConnectivityCheckService(new StubHttpClientFactory(_handler), new ConnectionStateService(new AppEventService()), NullLogger<ConnectivityCheckService>.Instance);
    }

    [Fact]
    public async Task ThePublicIpComesBackWithAPassedCheck()
    {
        _handler.ResponseBody = "203.0.113.9\n";

        var check = await _connectivity.CheckAsync(Settings([]), TestContext.Current.CancellationToken);

        Assert.Equal((true, null, "203.0.113.9"), (check.Proceed, check.SkipReason, check.PublicIp));
    }

    [Fact]
    public async Task ASkippedTestStillKnowsThePublicIp()
    {
        _handler.ResponseBody = "203.0.113.9";

        var check = await _connectivity.CheckAsync(Settings(["203.0.113.9"]), TestContext.Current.CancellationToken);

        Assert.Equal((false, "Public IP 203.0.113.9 is on the skip list", "203.0.113.9"), (check.Proceed, check.SkipReason, check.PublicIp));
    }

    [Fact]
    public async Task ACheckThatIsTurnedOffLooksUpNothing()
    {
        var check = await _connectivity.CheckAsync(new PreTestCheckSettings(false, "https://localhost/ip", []), TestContext.Current.CancellationToken);

        Assert.Equal((true, null), (check.Proceed, check.PublicIp));
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task AMonitorThatHasTheLineDownStopsTheTestBeforeAnyLookup()
    {
        var connection = new ConnectionStateService(new AppEventService());
        connection.Update(ConnectionHealth.Down, DateTime.UtcNow, null);
        var checker = new ConnectivityCheckService(new StubHttpClientFactory(_handler), connection, NullLogger<ConnectivityCheckService>.Instance);

        var check = await checker.CheckAsync(Settings([]), TestContext.Current.CancellationToken);

        Assert.Equal((false, "No internet connection: the monitor has the line down"), (check.Proceed, check.SkipReason));
        Assert.Empty(_handler.Requests);
    }

    private static PreTestCheckSettings Settings(IReadOnlyList<string> skipIps) => new(true, "https://localhost/ip", skipIps);

    public void Dispose() => _handler.Dispose();
}
