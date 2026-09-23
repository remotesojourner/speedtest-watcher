using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.UnitTests.Application.Monitoring;

public class BufferbloatTests
{
    private static readonly BufferbloatSampling _sampling = BufferbloatSampling.Default;
    private static readonly TimeSpan _longEnough = TimeSpan.FromSeconds(12);

    [Fact]
    public void BufferbloatIsTheMedianUnderLoadLessTheMedianIdle()
    {
        var reading = From([10, 12, 11, 13, 12], Under(LoadDirection.Download, 12, 50));

        Assert.NotNull(reading);
        Assert.Equal((38, 12, 50), (reading.DownloadMilliseconds, reading.IdleMilliseconds, reading.LoadedMilliseconds));
    }

    [Fact]
    public void ALineThatAnswersFasterUnderLoadCountsAsNoBufferbloat()
    {
        var reading = From(Flat(6, 20), Under(LoadDirection.Download, 12, 12));

        Assert.Equal(0, reading!.DownloadMilliseconds);
        Assert.Equal(12, reading.LoadedMilliseconds);
    }

    [Fact]
    public void EachDirectionIsMeasuredAgainstTheSameIdleBaseline()
    {
        IReadOnlyList<LatencySample> loaded =
        [
            .. Under(LoadDirection.Download, 6, 20),
            .. Under(LoadDirection.Upload, 6, 80)
        ];

        var reading = From(Flat(6, 12), loaded);

        Assert.Equal((8, 68), (reading!.DownloadMilliseconds, reading.UploadMilliseconds));
    }

    [Fact]
    public void ADirectionWithTooFewSamplesHasNoFigureOfItsOwn()
    {
        IReadOnlyList<LatencySample> loaded =
        [
            .. Under(LoadDirection.Download, 10, 20),
            .. Under(LoadDirection.Upload, 3, 80)
        ];

        var reading = From(Flat(6, 12), loaded);

        Assert.Equal(8, reading!.DownloadMilliseconds);
        Assert.Null(reading.UploadMilliseconds);
    }

    [Fact]
    public void SamplesTheAppCannotPlaceGiveNoBufferbloatFigureButStillCountAsLoadedLatency()
    {
        var reading = From(Flat(6, 12), Under(LoadDirection.Unclear, 12, 60));

        Assert.Equal(60, reading!.LoadedMilliseconds);
        Assert.Null(reading.DownloadMilliseconds);
        Assert.Null(reading.UploadMilliseconds);
    }

    [Theory]
    [InlineData(80_000_000, 1_000_000, LoadDirection.Download)]
    [InlineData(1_000_000, 80_000_000, LoadDirection.Upload)]
    [InlineData(40_000_000, 30_000_000, LoadDirection.Unclear)]
    [InlineData(2_000, 1_000, LoadDirection.Unclear)]
    public void TheDirectionComesFromWhichWayTheBytesWent(long received, long sent, LoadDirection expected)
    {
        Assert.Equal(expected, Bufferbloat.DirectionOf(new TrafficCounters(received, sent), _sampling.BytesThatMeanTransfer));
    }

    [Fact]
    public void TheTailIsTheWorstTwentiethOfTheLoadedSamples()
    {
        var oneBlip = From(Flat(6, 12), [.. Under(LoadDirection.Unclear, 19, 40), Sample(900)]);
        var twoBlips = From(Flat(6, 12), [.. Under(LoadDirection.Unclear, 18, 40), Sample(900), Sample(900)]);

        Assert.Equal((40, 40), (oneBlip!.LoadedMilliseconds, oneBlip.LoadedTailMilliseconds));
        Assert.Equal((40, 900), (twoBlips!.LoadedMilliseconds, twoBlips.LoadedTailMilliseconds));
    }

    [Fact]
    public void ARetransmittedHandshakeIsKeptOutOfTheBaseline()
    {
        var withRetransmit = From([12, 11, 13, 12, 1013, 12], Under(LoadDirection.Download, 12, 50));
        var without = From([12, 11, 13, 12, 12], Under(LoadDirection.Download, 12, 50));

        Assert.Equal(without!.IdleMilliseconds, withRetransmit!.IdleMilliseconds);
        Assert.Equal(without.DownloadMilliseconds, withRetransmit.DownloadMilliseconds);
    }

    [Fact]
    public void ARetransmittedHandshakeIsTheSignalUnderLoadAndStays()
    {
        var reading = From(Flat(6, 12), Under(LoadDirection.Upload, 10, 1013));

        Assert.Equal(1001, reading!.UploadMilliseconds);
    }

    [Fact]
    public void ABaselineThatIsAllRetransmitsGivesNoFigureAtAll()
    {
        Assert.Null(From([1013, 1014, 1013, 1015, 1013, 1014], Under(LoadDirection.Unclear, 12, 50)));
    }

    [Fact]
    public void ASlowLinkKeepsItsOwnSamplesAndDropsOnlyItsRetransmits()
    {
        var satellite = new List<double> { 600, 610, 620, 605, 615, 1615 };

        var reading = From(satellite, Under(LoadDirection.Download, 12, 700));

        Assert.Equal((610, 90), (reading!.IdleMilliseconds, reading.DownloadMilliseconds));
    }

    [Theory]
    [InlineData(4, 12)]
    [InlineData(6, 9)]
    public void TooFewSamplesEitherSideMeansNoFigureAtAll(int idle, int loaded)
    {
        Assert.Null(From(Flat(idle, 12), Under(LoadDirection.Unclear, loaded, 50)));
    }

    [Fact]
    public void ATestTooShortToSaturateTheLineIsNotMeasured()
    {
        Assert.Null(Bufferbloat.From(Flat(6, 12), Under(LoadDirection.Unclear, 12, 50), TimeSpan.FromSeconds(2), _sampling));
    }

    private static BufferbloatReading? From(IReadOnlyList<double> idle, IReadOnlyList<LatencySample> loaded) =>
        Bufferbloat.From(idle, loaded, _longEnough, _sampling);

    private static List<double> Flat(int count, double milliseconds) => [.. Enumerable.Repeat(milliseconds, count)];

    private static List<LatencySample> Under(LoadDirection direction, int count, double milliseconds) =>
        [.. Enumerable.Repeat(new LatencySample(milliseconds, direction), count)];

    private static LatencySample Sample(double milliseconds) => new(milliseconds, LoadDirection.Unclear);
}
