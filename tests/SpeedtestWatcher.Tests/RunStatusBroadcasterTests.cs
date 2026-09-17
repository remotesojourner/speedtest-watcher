using System.Collections.Concurrent;
using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Web.Background;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Tests;

public sealed class RunStatusBroadcasterTests : IDisposable
{
    private readonly PauseStateService _status = new();
    private readonly ConcurrentQueue<(bool Running, bool Paused)> _broadcasts = new();
    private readonly SemaphoreSlim _broadcastArrived = new(0);
    private readonly RunStatusBroadcaster _broadcaster;

    public RunStatusBroadcasterTests()
    {
        var clients = A.Fake<IHubClients>();
        var everyone = A.Fake<IClientProxy>();
        var hub = A.Fake<IHubContext<SpeedtestHub>>();
        A.CallTo(() => hub.Clients).Returns(clients);
        A.CallTo(() => clients.All).Returns(everyone);
        A.CallTo(() => everyone.SendCoreAsync("StatusChanged", A<object?[]>._, A<CancellationToken>._))
            .Invokes((string _, object?[] args, CancellationToken _) =>
            {
                _broadcasts.Enqueue(((bool)args[0]!, (bool)args[1]!));
                _broadcastArrived.Release();
            });

        _broadcaster = new RunStatusBroadcaster(_status, hub);
    }

    [Fact]
    public async Task PausingResumingAndRunning_AreBroadcastToEveryone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _broadcaster.StartAsync(cancellationToken);

        _status.Pause(null);
        _status.Resume();
        _status.SetRunning(true);
        _status.SetRunning(false);

        Assert.Equal([(false, true), (false, false), (true, false), (false, false)], _broadcasts);
    }

    [Fact(Timeout = 15000)]
    public async Task ATimedPauseRunningOut_IsBroadcast()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _broadcaster.StartAsync(cancellationToken);

        _status.Pause(TimeSpan.FromMilliseconds(200).TotalHours);
        await _broadcastArrived.WaitAsync(cancellationToken);
        await _broadcastArrived.WaitAsync(cancellationToken);

        Assert.Equal([(false, true), (false, false)], _broadcasts);
    }

    [Fact]
    public async Task NothingIsBroadcast_AfterTheAppStops()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _broadcaster.StartAsync(cancellationToken);
        await _broadcaster.StopAsync(cancellationToken);

        _status.Pause(null);

        Assert.Empty(_broadcasts);
    }

    public void Dispose()
    {
        _status.Dispose();
        _broadcastArrived.Dispose();
    }
}
