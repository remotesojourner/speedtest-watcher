using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.UnitTests.Web.Api;

public sealed class LinkPreviewTests
{
    [Fact]
    public void ThePreviewShowsTheLatestCompletedTest()
    {
        var preview = LinkPreview.For(new Speedtest
        {
            Ping = 12, Jitter = 0.44, Download = 941.26, Upload = 110.5, Created = new DateTime(2026, 9, 14, 8, 5, 0)
        });

        Assert.Equal(new LinkPreview("Latest test: 2026-09-14 08:05:00 UTC", "12 ms", "±0.4 ms jitter", "941.3", "110.5"), preview);
    }

    [Fact]
    public void BeforeAnyTestHasCompletedThePreviewShowsNoReadings()
    {
        var preview = LinkPreview.For(null);

        Assert.Equal(new LinkPreview("No completed speedtests yet", "--", null, "--", "--"), preview);
    }
}
