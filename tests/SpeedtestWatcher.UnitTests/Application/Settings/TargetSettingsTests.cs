using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests.Application.Settings;

public class TargetSettingsTests
{
    private static readonly TargetSettings _targets = new(Ping: 25, Download: 900, Upload: 100);

    private static readonly TargetSettings _everyTarget =
        new(Ping: 25, Download: 900, Upload: 100, PacketLoss: 0.5, Bufferbloat: 30);

    [Fact]
    public void EveryMissedTargetIsListedInTheOrderPingDownloadUpload()
    {
        Assert.Equal([TargetKind.Ping, TargetKind.Download, TargetKind.Upload], _targets.Missed(Reading(31, 612.5, 98.125)));
        Assert.Equal([TargetKind.Upload], _targets.Missed(Reading(25, 900, 99.9)));
        Assert.Empty(_targets.Missed(Reading(25, 900, 100)));
    }

    [Fact]
    public void AResultIsHealthyExactlyWhenItMissesNoTarget()
    {
        Assert.True(_targets.Evaluate(Reading(25, 900, 100)));
        Assert.False(_targets.Evaluate(Reading(26, 900, 100)));
        Assert.Null(new TargetSettings(null, null, null).Evaluate(Reading(500, 1, 1)));
    }

    [Fact]
    public void TargetsWithoutAValueAreNeverMissed()
    {
        Assert.Equal([TargetKind.Download], new TargetSettings(null, 900, null).Missed(Reading(500, 10, 1)));
    }

    [Fact]
    public void PacketLossAndBufferbloatAreJudgedWhenTheyWereMeasured()
    {
        var healthy = _everyTarget.Missed(Reading(12, 941, 110, packetLoss: 0, bufferbloatDown: 12, bufferbloatUp: 12));
        var loose = _everyTarget.Missed(Reading(12, 941, 110, packetLoss: 1.25, bufferbloatDown: 12, bufferbloatUp: 12));
        var bloated = _everyTarget.Missed(Reading(12, 941, 110, packetLoss: 0, bufferbloatDown: 140, bufferbloatUp: 12));

        Assert.Empty(healthy);
        Assert.Equal([TargetKind.PacketLoss], loose);
        Assert.Equal([TargetKind.Bufferbloat], bloated);
    }

    [Fact]
    public void TheBufferbloatMaximumAppliesToEachDirectionOnItsOwn()
    {
        Assert.Equal([TargetKind.Bufferbloat], _everyTarget.Missed(Reading(12, 941, 110, bufferbloatDown: 5, bufferbloatUp: 45)));
        Assert.Equal([TargetKind.Bufferbloat], _everyTarget.Missed(Reading(12, 941, 110, bufferbloatUp: 45)));
        Assert.Empty(_everyTarget.Missed(Reading(12, 941, 110, bufferbloatDown: 25)));
    }

    [Fact]
    public void AFigureThatWasNeverMeasuredIsNeverAMiss()
    {
        Assert.Empty(_everyTarget.Missed(Reading(12, 941, 110)));
        Assert.True(_everyTarget.Evaluate(Reading(12, 941, 110)));
    }

    [Fact]
    public void AMaximumOfZeroMeansAnyLossAtAllIsAMiss()
    {
        var noLossAllowed = new TargetSettings(null, null, null, PacketLoss: 0);

        Assert.Empty(noLossAllowed.Missed(Reading(12, 941, 110, packetLoss: 0)));
        Assert.Equal([TargetKind.PacketLoss], noLossAllowed.Missed(Reading(12, 941, 110, packetLoss: 0.1)));
    }

    [Theory]
    [InlineData(new TargetKind[0], "")]
    [InlineData(new[] { TargetKind.Upload }, "upload")]
    [InlineData(new[] { TargetKind.Download, TargetKind.Upload }, "download and upload")]
    [InlineData(new[] { TargetKind.Ping, TargetKind.Download, TargetKind.Upload }, "ping, download and upload")]
    [InlineData(new[] { TargetKind.PacketLoss, TargetKind.Bufferbloat }, "packet loss and bufferbloat")]
    public void MissedTargetsReadAsAList(TargetKind[] missed, string expected)
    {
        Assert.Equal(expected, TargetSettings.Describe(missed));
    }

    private static Readings Reading(
        int ping, double download, double upload, double? packetLoss = null, double? bufferbloatDown = null, double? bufferbloatUp = null) =>
        new(ping, download, upload, packetLoss, bufferbloatDown, bufferbloatUp);
}
