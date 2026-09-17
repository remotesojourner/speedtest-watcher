using FakeItEasy;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Web.Helpers;

namespace SpeedtestWatcher.Tests;

public sealed class LinkPreviewTests
{
    private readonly ISpeedtestRepository _results = A.Fake<ISpeedtestRepository>();

    [Fact]
    public async Task ThePreviewShowsTheLatestCompletedTest_NotALaterFailedOne()
    {
        A.CallTo(() => _results.GetLatestAsync(A<CancellationToken>._)).Returns(new Speedtest
        {
            Status = "failed", Ping = -1, Download = -1, Upload = -1, Created = new DateTime(2026, 9, 15, 7, 0, 0)
        });
        A.CallTo(() => _results.GetLatestCompletedAsync(A<CancellationToken>._)).Returns(new Speedtest
        {
            Ping = 12, Jitter = 0.44, Download = 941.26, Upload = 110.5, Created = new DateTime(2026, 9, 14, 8, 5, 0)
        });

        var preview = await LinkPreview.ForLatestCompletedTestAsync(_results, TestContext.Current.CancellationToken);

        Assert.Equal(new LinkPreview("Latest test: 2026-09-14 08:05:00 UTC", "12 ms", "±0.4 ms jitter", "941.3", "110.5"), preview);
    }

    [Fact]
    public async Task BeforeAnyTestHasCompleted_ThePreviewShowsNoReadings()
    {
        A.CallTo(() => _results.GetLatestCompletedAsync(A<CancellationToken>._)).Returns((Speedtest?)null);

        var preview = await LinkPreview.ForLatestCompletedTestAsync(_results, TestContext.Current.CancellationToken);

        Assert.Equal(new LinkPreview("No completed speedtests yet", "--", null, "--", "--"), preview);
    }
}
