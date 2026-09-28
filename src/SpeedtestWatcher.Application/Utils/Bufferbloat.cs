using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Utils;

public static class Bufferbloat
{
    private const double RetransmitGuardMilliseconds = 500;
    private const double RetransmitFloorMilliseconds = 1000;

    public static LoadDirection DirectionOf(TrafficCounters moved, long bytesThatMeanTransfer)
    {
        if (moved.Received < bytesThatMeanTransfer && moved.Sent < bytesThatMeanTransfer) return LoadDirection.Unclear;
        if (moved.Received > moved.Sent * 2) return LoadDirection.Download;
        return moved.Sent > moved.Received * 2 ? LoadDirection.Upload : LoadDirection.Unclear;
    }

    public static BufferbloatReading? From(
        IReadOnlyList<double> idle,
        IReadOnlyList<LatencySample> loaded,
        TimeSpan loadedFor,
        BufferbloatSampling sampling)
    {
        var baseline = WithoutRetransmits(idle);
        if (baseline.Count < sampling.LeastIdleSamples) return null;
        if (loaded.Count < sampling.LeastLoadSamples || loadedFor < sampling.LeastLoadTime) return null;

        var idleMedian = Median(baseline);
        var everything = loaded.Select(sample => sample.Milliseconds).ToList();

        return new BufferbloatReading(
            idleMedian,
            Median(everything),
            Percentile95(everything),
            BloatWhile(LoadDirection.Download, loaded, idleMedian, sampling),
            BloatWhile(LoadDirection.Upload, loaded, idleMedian, sampling));
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

    private static double? BloatWhile(LoadDirection direction, IReadOnlyList<LatencySample> loaded, double idleMedian, BufferbloatSampling sampling)
    {
        var samples = loaded.Where(sample => sample.Direction == direction).Select(sample => sample.Milliseconds).ToList();
        return samples.Count < sampling.LeastDirectionSamples ? null : Above(idleMedian, Median(samples));
    }

    private static double Above(double idleMedian, double loadedMedian) => Math.Max(0, loadedMedian - idleMedian);
}
