using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests.Application.Settings;

public class TargetSettingsTests
{
    private static readonly TargetSettings _targets = new(Ping: 25, Download: 900, Upload: 100);

    [Fact]
    public void EveryMissedTargetIsListedInTheOrderPingDownloadUpload()
    {
        Assert.Equal([TargetKind.Ping, TargetKind.Download, TargetKind.Upload], _targets.Missed(31, 612.5, 98.125));
        Assert.Equal([TargetKind.Upload], _targets.Missed(25, 900, 99.9));
        Assert.Empty(_targets.Missed(25, 900, 100));
    }

    [Fact]
    public void AResultIsHealthyExactlyWhenItMissesNoTarget()
    {
        Assert.True(_targets.Evaluate(25, 900, 100));
        Assert.False(_targets.Evaluate(26, 900, 100));
        Assert.Null(new TargetSettings(null, null, null).Evaluate(500, 1, 1));
    }

    [Fact]
    public void TargetsWithoutAValueAreNeverMissed()
    {
        Assert.Equal([TargetKind.Download], new TargetSettings(null, 900, null).Missed(500, 10, 1));
    }

    [Theory]
    [InlineData(new TargetKind[0], "")]
    [InlineData(new[] { TargetKind.Upload }, "upload")]
    [InlineData(new[] { TargetKind.Download, TargetKind.Upload }, "download and upload")]
    [InlineData(new[] { TargetKind.Ping, TargetKind.Download, TargetKind.Upload }, "ping, download and upload")]
    public void MissedTargetsReadAsAList(TargetKind[] missed, string expected)
    {
        Assert.Equal(expected, TargetSettings.Describe(missed));
    }
}
