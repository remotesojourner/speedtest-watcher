using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests.Application.Monitoring;

public class OutageTrackerTests
{
    private static readonly MonitoringSettings _settings =
        new(Enabled: true, Targets: ProbeTarget.ParseList(["1.1.1.1:443"]), IntervalSeconds: 15, RoundsToGoDown: 3, RoundsToGoUp: 2, TestAfterReconnect: false);

    private readonly OutageTracker _tracker = new();

    [Fact]
    public void ThreeFailedRoundsTakeTheLineDownAndTwoGoodOnesBringItBack()
    {
        Assert.Equal(
            [ConnectionChange.None, ConnectionChange.None, ConnectionChange.WentDown, ConnectionChange.None, ConnectionChange.CameUp],
            Record([false, false, false, true, true]));
        Assert.Equal(ConnectionHealth.Up, _tracker.Health);
    }

    [Fact]
    public void OneFailedRoundIsNotAnOutage()
    {
        Assert.All(Record([true, true, false, true, true]), change => Assert.Equal(ConnectionChange.None, change));
        Assert.Equal(ConnectionHealth.Up, _tracker.Health);
    }

    [Fact]
    public void FailuresHaveToBeInARow()
    {
        Assert.All(Record([false, false, true, false, false, true]), change => Assert.Equal(ConnectionChange.None, change));
    }

    [Fact]
    public void TheFirstGoodRoundsAreNotAComeback()
    {
        Assert.Equal([ConnectionChange.None, ConnectionChange.None], Record([true, true]));
        Assert.Equal(ConnectionHealth.Up, _tracker.Health);
    }

    [Fact]
    public void ForgettingLeavesTheLineUnknownAgain()
    {
        Record([false, false, false]);

        _tracker.Forget();

        Assert.Equal(ConnectionHealth.Unknown, _tracker.Health);
    }

    private List<ConnectionChange> Record(bool[] rounds) =>
        [.. rounds.Select(passed => _tracker.Record(passed, _settings))];
}
