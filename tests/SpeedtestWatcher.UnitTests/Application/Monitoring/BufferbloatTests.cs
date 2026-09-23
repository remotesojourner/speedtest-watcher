using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.UnitTests.Application.Monitoring;

public class BufferbloatTests
{
    private static readonly BufferbloatSampling _sampling = BufferbloatSampling.Default;
    private static readonly TimeSpan _longEnough = TimeSpan.FromSeconds(12);

    [Fact]
    public void BufferbloatIsTheMedianUnderLoadLessTheMedianIdle()
    {
        var reading = From([10, 12, 11, 13, 12], Samples(12, 50));

        Assert.NotNull(reading);
        Assert.Equal((38, 12, 50), (reading.Milliseconds, reading.IdleMilliseconds, reading.LoadedMilliseconds));
    }

    [Fact]
    public void ALineThatAnswersFasterUnderLoadCountsAsNoBufferbloat()
    {
        var reading = From(Samples(6, 20), Samples(12, 12));

        Assert.Equal(0, reading!.Milliseconds);
        Assert.Equal(12, reading.LoadedMilliseconds);
    }

    [Fact]
    public void TheTailIsTheWorstTwentiethOfTheLoadedSamples()
    {
        var oneBlip = From(Samples(6, 12), [.. Samples(19, 40), 900]);
        var twoBlips = From(Samples(6, 12), [.. Samples(18, 40), 900, 900]);

        Assert.Equal((40, 40), (oneBlip!.LoadedMilliseconds, oneBlip.LoadedTailMilliseconds));
        Assert.Equal((40, 900), (twoBlips!.LoadedMilliseconds, twoBlips.LoadedTailMilliseconds));
    }

    [Fact]
    public void ARetransmittedHandshakeIsKeptOutOfTheBaseline()
    {
        var withRetransmit = From([12, 11, 13, 12, 1013, 12], Samples(12, 50));
        var without = From([12, 11, 13, 12, 12], Samples(12, 50));

        Assert.Equal(without!.IdleMilliseconds, withRetransmit!.IdleMilliseconds);
        Assert.Equal(without.Milliseconds, withRetransmit.Milliseconds);
    }

    [Fact]
    public void ARetransmittedHandshakeIsTheSignalUnderLoadAndStays()
    {
        var reading = From(Samples(6, 12), [1013, 1014, 1013, 1015, 1013, 1014, 1013, 1015, 1013, 1014]);

        Assert.Equal(1001.5, reading!.Milliseconds);
    }

    [Fact]
    public void ABaselineThatIsAllRetransmitsGivesNoFigureAtAll()
    {
        Assert.Null(From([1013, 1014, 1013, 1015, 1013, 1014], Samples(12, 50)));
    }

    [Fact]
    public void ASlowLinkKeepsItsOwnSamplesAndDropsOnlyItsRetransmits()
    {
        var satellite = new List<double> { 600, 610, 620, 605, 615, 1615 };

        var reading = From(satellite, Samples(12, 700));

        Assert.Equal((610, 90), (reading!.IdleMilliseconds, reading.Milliseconds));
    }

    [Theory]
    [InlineData(4, 12)]
    [InlineData(6, 9)]
    public void TooFewSamplesEitherSideMeansNoFigureAtAll(int idle, int loaded)
    {
        Assert.Null(From(Samples(idle, 12), Samples(loaded, 50)));
    }

    [Fact]
    public void ATestTooShortToSaturateTheLineIsNotMeasured()
    {
        var reading = Bufferbloat.From(Samples(6, 12), Samples(12, 50), TimeSpan.FromSeconds(2), _sampling);

        Assert.Null(reading);
    }

    private static BufferbloatReading? From(IReadOnlyList<double> idle, IReadOnlyList<double> loaded) =>
        Bufferbloat.From(idle, loaded, _longEnough, _sampling);

    private static List<double> Samples(int count, double milliseconds) =>
        [.. Enumerable.Repeat(milliseconds, count)];
}
