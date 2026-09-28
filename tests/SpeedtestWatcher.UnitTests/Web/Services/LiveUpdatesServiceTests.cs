using FakeItEasy;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Web.Services;

namespace SpeedtestWatcher.UnitTests.Web.Services;

public sealed class LiveUpdatesServiceTests : IDisposable
{
    private readonly AppEventService _events = new();
    private readonly RunStateService _runState;
    private readonly ISettingsRepository _settingsRepository = A.Fake<ISettingsRepository>();
    private readonly StatusStateService _status = new();
    private readonly RecentResultsService _recent;
    private readonly SettingsStateService _settings;
    private readonly RecordingLogger<LiveUpdatesService> _logger = new();
    private readonly LiveUpdatesService _live;
    private readonly List<SpeedtestDto> _announced = [];

    public LiveUpdatesServiceTests()
    {
        _runState = new RunStateService(_events);
        _recent = new RecentResultsService(new ResultsService(A.Fake<ISpeedtestRepository>(), FixedAccess.Full));
        _settings = new SettingsStateService(new SettingsService(_settingsRepository, A.Fake<IIntegrationDispatchService>(), _events, A.Fake<ISignInStateService>(), FixedAccess.Full));
        _live = new LiveUpdatesService(_events, new ConnectionStateService(_events), _status, _recent, _settings, FixedAccess.Full, _logger);
        _live.ResultArrived += _announced.Add;
    }

    [Fact]
    public void AReadOnlyVisitorIsNotToldThePublicIp()
    {
        var visitor = new LiveUpdatesService(_events, new ConnectionStateService(_events), _status, _recent, _settings, FixedAccess.ReadOnly, _logger);
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
        A.CallTo(() => _settingsRepository.GetAsync(A<CancellationToken>._))
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
