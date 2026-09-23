using FakeItEasy;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Web.Ui.State;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public sealed class LiveUpdatesTests : IDisposable
{
    private readonly AppEvents _events = new();
    private readonly RunState _runState;
    private readonly ISettingsStore _store = A.Fake<ISettingsStore>();
    private readonly StatusStateService _status = new();
    private readonly RecentResults _recent;
    private readonly SettingsState _settings;
    private readonly RecordingLogger<LiveUpdates> _logger = new();
    private readonly LiveUpdates _live;
    private readonly List<SpeedtestDto> _announced = [];

    public LiveUpdatesTests()
    {
        _runState = new RunState(_events);
        _recent = new RecentResults(new ResultsService(A.Fake<ISpeedtestRepository>(), FixedAccess.Full));
        _settings = new SettingsState(new SettingsService(_store, A.Fake<IIntegrationDispatcher>(), _events, A.Fake<ISignInState>(), FixedAccess.Full));
        _live = new LiveUpdates(_events, new ConnectionState(_events), _status, _recent, _settings, FixedAccess.Full, _logger);
        _live.ResultArrived += _announced.Add;
    }

    [Fact]
    public void AReadOnlyVisitorIsNotToldThePublicIp()
    {
        var visitor = new LiveUpdates(_events, new ConnectionState(_events), _status, _recent, _settings, FixedAccess.ReadOnly, _logger);
        var announced = new List<SpeedtestDto>();
        visitor.ResultArrived += announced.Add;
        visitor.Start(work => work());
        var published = new SpeedtestDto { Id = 7, PublicIp = "203.0.113.9" };

        _events.PublishTestFinished(published);

        Assert.Null(Assert.Single(announced).PublicIp);
        Assert.Equal("203.0.113.9", published.PublicIp);
        visitor.Dispose();
    }

    [Fact]
    public void AFinishedTestIsAddedToTheRecentResultsAndAnnounced()
    {
        _live.Start(RunNow);
        var result = new SpeedtestDto { Id = 7, Status = TestStatus.Completed };

        _events.PublishTestStarted();
        Assert.True(_status.Running);

        _events.PublishTestFinished(result);

        Assert.False(_status.Running);
        Assert.Same(result, _recent.Latest);
        Assert.Equal([result], _announced);
    }

    [Fact]
    public void PausingAndResumingReachEveryTab()
    {
        _live.Start(RunNow);

        _runState.Pause(null);
        Assert.True(_status.Paused);

        _runState.Resume();
        Assert.False(_status.Paused);
    }

    [Fact]
    public void ASettingsChangeReloadsTheSettings()
    {
        _live.Start(RunNow);
        A.CallTo(() => _store.GetAsync(A<CancellationToken>._))
            .Returns(AppSettings.From(new Dictionary<string, string> { ["chartRange"] = "24h" }));

        _events.PublishSettingsChanged(new Dictionary<string, string> { ["chartRange"] = "24h" });

        Assert.Equal("24h", _settings.Current.Display.ChartRange);
    }

    [Fact]
    public void UpdatesRunThroughTheTabsDispatcher()
    {
        var dispatched = 0;
        _live.Start(work =>
        {
            dispatched++;
            return work();
        });

        _events.PublishTestStarted();
        _runState.Pause(null);

        Assert.Equal(2, dispatched);
    }

    [Fact]
    public void AnUpdateThatFailsIsLoggedAndLaterUpdatesStillArrive()
    {
        var failNext = true;
        _live.Start(work =>
        {
            if (!failNext) return work();
            failNext = false;
            throw new InvalidOperationException("The circuit is gone");
        });

        _events.PublishTestStarted();
        _runState.Pause(null);

        Assert.Single(_logger.Warnings);
        Assert.True(_status.Paused);
    }

    [Fact]
    public void NothingArrivesBeforeStartOrAfterDispose()
    {
        _events.PublishTestStarted();
        Assert.False(_status.Running);

        _live.Start(RunNow);
        _live.Dispose();
        _events.PublishTestFinished(new SpeedtestDto { Id = 1 });

        Assert.Empty(_recent.Tests);
        Assert.Empty(_announced);
    }

    private static Task RunNow(Func<Task> work) => work();

    public void Dispose()
    {
        _live.Dispose();
        _runState.Dispose();
    }
}
