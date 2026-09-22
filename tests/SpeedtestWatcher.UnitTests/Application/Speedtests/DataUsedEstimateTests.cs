using FakeItEasy;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.UnitTests.Application.Speedtests;

public class DataUsedEstimateTests
{
    private readonly ISpeedtestRepository _results = A.Fake<ISpeedtestRepository>();

    [Fact]
    public async Task TheEstimateIsTheMedianRunTimesTheRunsAMonth()
    {
        Recent([1_000_000_000, 1_100_000_000, 900_000_000]);

        var estimate = await EstimateAsync("0 * * * *", TimeSpan.FromDays(30));

        Assert.Equal(1_000_000_000L * 720, estimate);
    }

    [Fact]
    public async Task AnHourOfTestingEveryFiveMinutesCostsTwelveRuns()
    {
        Recent([1_000_000_000]);

        var estimate = await EstimateAsync("*/5 * * * *", TimeSpan.FromHours(1));

        Assert.Equal(1_000_000_000L * 12, estimate);
    }

    [Fact]
    public async Task WithoutRunsToLearnFromThereIsNoEstimate()
    {
        Recent([]);

        Assert.Null(await EstimateAsync("0 * * * *", TimeSpan.FromDays(30)));
    }

    [Fact]
    public async Task AnExpressionThatIsNotCronHasNoEstimate()
    {
        Recent([1_000_000_000]);

        Assert.Null(await EstimateAsync("every so often", TimeSpan.FromDays(30)));
    }

    private void Recent(long[] bytes) =>
        A.CallTo(() => _results.RecentRunBytesAsync(A<int>._, A<CancellationToken>._)).Returns(bytes);

    private Task<long?> EstimateAsync(string cron, TimeSpan window) =>
        new ResultsService(_results, FixedAccess.Full).EstimateBytesAsync(new ScheduleSettings(cron, RandomOffset: false), window, TestContext.Current.CancellationToken);
}
