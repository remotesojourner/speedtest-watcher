using FakeItEasy;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.UnitTests.Application.Services;

public sealed class RunStateServiceTests : IDisposable
{
    private readonly RunStateService _state = new(A.Fake<IAppEventService>());

    [Fact]
    public void SkippingTheNextScheduledRunPausesUntilTheSchedulerReachesIt()
    {
        _state.SkipNextScheduledRun();

        Assert.True(_state.IsPaused);
        Assert.True(_state.TrySkipScheduledRun());
        Assert.False(_state.IsPaused);
        Assert.False(_state.TrySkipScheduledRun());
    }

    [Fact]
    public void AnotherPauseReplacesASkipSoTheSchedulerDoesNotEndIt()
    {
        _state.SkipNextScheduledRun();
        _state.Pause(null);

        Assert.False(_state.TrySkipScheduledRun());
        Assert.True(_state.IsPaused);
    }

    [Fact]
    public void ResumingCancelsASkip()
    {
        _state.SkipNextScheduledRun();
        _state.Resume();

        Assert.False(_state.TrySkipScheduledRun());
        Assert.False(_state.IsPaused);
    }

    [Fact]
    public void ScheduledRunsAreNotSkippedWithoutBeingAsked()
    {
        Assert.False(_state.TrySkipScheduledRun());

        _state.Pause(null);

        Assert.False(_state.TrySkipScheduledRun());
    }

    public void Dispose() => _state.Dispose();
}
