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

        var estimate = await EstimateAsync("0 * * * *");

        Assert.Equal(1_000_000_000L * 720, estimate);
    }

    [Fact]
    public async Task WithoutRunsToLearnFromThereIsNoEstimate()
    {
        Recent([]);

        Assert.Null(await EstimateAsync("0 * * * *"));
    }

    [Fact]
    public async Task AnExpressionThatIsNotCronHasNoEstimate()
    {
        Recent([1_000_000_000]);

        Assert.Null(await EstimateAsync("every so often"));
    }

    private void Recent(long[] bytes) =>
        A.CallTo(() => _results.RecentRunBytesAsync(A<int>._, A<CancellationToken>._)).Returns(bytes);

    private Task<long?> EstimateAsync(string cron) =>
        new ResultsService(_results, FixedAccess.Full).EstimateMonthlyBytesAsync(new ScheduleSettings(cron, RandomOffset: false), TestContext.Current.CancellationToken);
}
