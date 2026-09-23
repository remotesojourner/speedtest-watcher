using System.Diagnostics;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Monitoring;

public sealed class LatencyHistoryTests : IDisposable
{
    private static readonly DateTime _noon = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    [Fact]
    public async Task RoundsAreAveragedIntoSlots()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StoreAsync(cancellationToken,
            Round(_noon, 10),
            Round(_noon.AddSeconds(15), 20),
            Round(_noon.AddSeconds(30), 30),
            Round(_noon.AddMinutes(5), 40));

        var points = await LatencyAsync(slotMinutes: 5, cancellationToken);

        Assert.Equal([_noon, _noon.AddMinutes(5)], points.Select(point => point.At));
        Assert.Equal([20, 40], points.Select(point => point.Milliseconds));
        Assert.Equal([3, 1], points.Select(point => point.Rounds));
    }

    [Fact]
    public async Task SlotsLongerThanAnHourSpanWholeHours()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var midnight = _noon.Date;
        await StoreAsync(cancellationToken,
            Round(midnight.AddMinutes(10), 10),
            Round(midnight.AddMinutes(110), 30),
            Round(midnight.AddHours(2), 50),
            Round(midnight.AddHours(3).AddMinutes(59), 70));

        await using var db = _database.NewContext();
        var points = await new MonitoringRepository(db).LatencyAsync(midnight, midnight.AddHours(4), 120, cancellationToken);

        Assert.Equal([midnight, midnight.AddHours(2)], points.Select(point => point.At));
        Assert.Equal([20, 60], points.Select(point => point.Milliseconds));
        Assert.Equal([2, 2], points.Select(point => point.Rounds));
    }

    [Fact]
    public async Task ASlotWhereNothingAnsweredHasNoFigureAndCountsItsFailures()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StoreAsync(cancellationToken, Failed(_noon), Failed(_noon.AddSeconds(15)), Round(_noon.AddMinutes(5), 12));

        var points = await LatencyAsync(slotMinutes: 5, cancellationToken);

        Assert.Null(points[0].Milliseconds);
        Assert.Equal((2, 2), (points[0].Rounds, points[0].Failed));
        Assert.Equal((1, 0), (points[1].Rounds, points[1].Failed));
    }

    [Fact]
    public async Task RoundsDuringASpeedtestAreCounted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StoreAsync(cancellationToken, Round(_noon, 10), Round(_noon.AddSeconds(15), 90, duringTest: true));

        var point = Assert.Single(await LatencyAsync(slotMinutes: 5, cancellationToken));

        Assert.Equal((2, 1), (point.Rounds, point.DuringTest));
        Assert.Equal(50, point.Milliseconds);
    }

    [Fact]
    public async Task OnlyTheRoundsInThePeriodAreAsked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await StoreAsync(cancellationToken, Round(_noon.AddDays(-8), 10), Round(_noon, 20));

        await using var db = _database.NewContext();
        var points = await new MonitoringRepository(db).LatencyAsync(_noon.AddDays(-7), _noon.AddHours(1), 30, cancellationToken);

        Assert.Equal(20, Assert.Single(points).Milliseconds);
    }

    [Fact]
    public async Task AWeekOfRoundsComesBackAsAFewHundredPoints()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var week = Enumerable.Range(0, 7 * 24 * 60 * 4)
            .Select(step => Round(_noon.AddDays(-7).AddSeconds(step * 15), 10 + (step % 5)))
            .ToArray();
        await StoreAsync(cancellationToken, week);

        var started = Stopwatch.GetTimestamp();
        await using var db = _database.NewContext();
        var points = await new MonitoringRepository(db).LatencyAsync(_noon.AddDays(-7), _noon, 30, cancellationToken);
        var took = Stopwatch.GetElapsedTime(started);

        Assert.Equal(40320, week.Length);
        Assert.InRange(points.Count, 300, 360);
        Assert.True(took < TimeSpan.FromSeconds(3), $"the query took {took.TotalSeconds:F1}s");
    }

    private async Task<List<LatencyPointDto>> LatencyAsync(int slotMinutes, CancellationToken cancellationToken)
    {
        await using var db = _database.NewContext();
        return await new MonitoringRepository(db).LatencyAsync(_noon.AddHours(-1), _noon.AddHours(1), slotMinutes, cancellationToken);
    }

    private async Task StoreAsync(CancellationToken cancellationToken, params ProbeRound[] rounds)
    {
        await using var db = _database.NewContext();
        db.ProbeRounds.AddRange(rounds);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static ProbeRound Round(DateTime at, double milliseconds, bool duringTest = false) => new()
    {
        At = at, Passed = true, Answered = 3, Asked = 3, FastestMilliseconds = milliseconds, DuringTest = duringTest
    };

    private static ProbeRound Failed(DateTime at) => new()
    {
        At = at, Passed = false, Answered = 0, Asked = 3, FastestMilliseconds = null, DuringTest = false
    };

    public void Dispose() => _database.Dispose();
}
