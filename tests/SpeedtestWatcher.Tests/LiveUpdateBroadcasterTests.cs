using System.Collections.Concurrent;
using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Web.Background;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Tests;

public sealed class LiveUpdateBroadcasterTests : IDisposable
{
    private readonly AppEvents _events = new();
    private readonly RunState _state;
    private readonly ConcurrentQueue<(string Method, object?[] Arguments)> _sent = new();
    private readonly SemaphoreSlim _sendArrived = new(0);
    private readonly LiveUpdateBroadcaster _broadcaster;

    public LiveUpdateBroadcasterTests()
    {
        _state = new RunState(_events);
        var clients = A.Fake<IHubClients>();
        var everyone = A.Fake<IClientProxy>();
        var hub = A.Fake<IHubContext<SpeedtestHub>>();
        A.CallTo(() => hub.Clients).Returns(clients);
        A.CallTo(() => clients.All).Returns(everyone);
        A.CallTo(() => everyone.SendCoreAsync(A<string>._, A<object?[]>._, A<CancellationToken>._))
            .Invokes((string method, object?[] arguments, CancellationToken _) =>
            {
                _sent.Enqueue((method, arguments));
                _sendArrived.Release();
            });

        _broadcaster = new LiveUpdateBroadcaster(_events, hub);
    }

    [Fact]
    public async Task PausingResumingAndRunning_AreBroadcastToEveryone()
    {
        await _broadcaster.StartAsync(TestContext.Current.CancellationToken);

        _state.Pause(null);
        _state.Resume();
        _state.TryStartRun();
        _state.FinishRun();

        Assert.Equal([(false, true), (false, false), (true, false), (false, false)], StatusChanges());
    }

    [Fact(Timeout = 15000)]
    public async Task ATimedPauseRunningOut_IsBroadcast()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _broadcaster.StartAsync(cancellationToken);

        _state.Pause(TimeSpan.FromMilliseconds(200).TotalHours);
        await _sendArrived.WaitAsync(cancellationToken);
        await _sendArrived.WaitAsync(cancellationToken);

        Assert.Equal([(false, true), (false, false)], StatusChanges());
    }

    [Fact]
    public async Task FailedResults_AreSentAsFailures_AndEverythingElseAsNewResults()
    {
        await _broadcaster.StartAsync(TestContext.Current.CancellationToken);

        _events.PublishTestStarted();
        _events.PublishTestFinished(new SpeedtestDto { Status = TestStatus.Completed });
        _events.PublishTestFinished(new SpeedtestDto { Status = TestStatus.Skipped });
        _events.PublishTestFinished(new SpeedtestDto { Status = TestStatus.Failed });

        Assert.Equal(["TestStarted", "NewTestResult", "NewTestResult", "TestFailed"], _sent.Select(message => message.Method));
    }

    [Fact]
    public async Task SettingsChanges_AreSentOneKeyAtATime()
    {
        await _broadcaster.StartAsync(TestContext.Current.CancellationToken);

        _events.PublishSettingsChanged(new Dictionary<string, string> { ["cron"] = "0,30 * * * *", ["chartRange"] = "24h" });

        Assert.Equal(
            [("ConfigChanged", "cron", "0,30 * * * *"), ("ConfigChanged", "chartRange", "24h")],
            _sent.Select(message => (message.Method, (string)message.Arguments[0]!, (string)message.Arguments[1]!)));
    }

    [Fact]
    public async Task NothingIsBroadcast_AfterTheAppStops()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _broadcaster.StartAsync(cancellationToken);
        await _broadcaster.StopAsync(cancellationToken);

        _state.Pause(null);
        _events.PublishTestStarted();

        Assert.Empty(_sent);
    }

    private List<(bool Running, bool Paused)> StatusChanges() =>
        _sent.Where(message => message.Method == "StatusChanged")
            .Select(message => ((bool)message.Arguments[0]!, (bool)message.Arguments[1]!))
            .ToList();

    public void Dispose()
    {
        _state.Dispose();
        _sendArrived.Dispose();
    }
}
