namespace SpeedtestWatcher.Application.Monitoring;

public sealed record BufferbloatReading(double Milliseconds, double IdleMilliseconds, double LoadedMilliseconds, double LoadedTailMilliseconds);

public sealed record BufferbloatSampling(
    TimeSpan IdleWindow,
    TimeSpan Cadence,
    TimeSpan Timeout,
    int LeastIdleSamples,
    int LeastLoadSamples,
    TimeSpan LeastLoadTime)
{
    public static BufferbloatSampling Default { get; } = new(
        IdleWindow: TimeSpan.FromSeconds(3),
        Cadence: TimeSpan.FromMilliseconds(250),
        Timeout: TimeSpan.FromSeconds(2),
        LeastIdleSamples: 5,
        LeastLoadSamples: 10,
        LeastLoadTime: TimeSpan.FromSeconds(3));
}

public static class Bufferbloat
{
    private const double RetransmitGuardMilliseconds = 500;
    private const double RetransmitFloorMilliseconds = 1000;

    public static BufferbloatReading? From(
        IReadOnlyList<double> idle,
        IReadOnlyList<double> loaded,
        TimeSpan loadedFor,
        BufferbloatSampling sampling)
    {
        var baseline = WithoutRetransmits(idle);
        if (baseline.Count < sampling.LeastIdleSamples) return null;
        if (loaded.Count < sampling.LeastLoadSamples || loadedFor < sampling.LeastLoadTime) return null;

        var idleMedian = Median(baseline);
        var loadedMedian = Median(loaded);
        return new BufferbloatReading(Math.Max(0, loadedMedian - idleMedian), idleMedian, loadedMedian, Percentile95(loaded));
    }

    public static IReadOnlyList<double> WithoutRetransmits(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0) return [];

        var quickest = samples.Min();
        if (quickest >= RetransmitFloorMilliseconds) return [];

        return [.. samples.Where(sample => sample <= quickest + RetransmitGuardMilliseconds)];
    }

    public static double Median(IReadOnlyList<double> samples)
    {
        var sorted = samples.Order().ToList();
        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
    }

    public static double Percentile95(IReadOnlyList<double> samples)
    {
        var sorted = samples.Order().ToList();
        return sorted[Math.Max((int)Math.Ceiling(0.95 * sorted.Count) - 1, 0)];
    }
}
