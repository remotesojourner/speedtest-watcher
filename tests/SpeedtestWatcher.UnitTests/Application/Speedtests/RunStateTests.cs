using FakeItEasy;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.UnitTests.Application.Speedtests;

public sealed class RunStateTests : IDisposable
{
    private readonly RunState _state = new(A.Fake<IAppEvents>());

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
