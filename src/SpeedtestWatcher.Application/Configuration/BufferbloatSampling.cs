namespace SpeedtestWatcher.Application.Configuration;

public sealed record BufferbloatSampling(
    TimeSpan IdleWindow,
    TimeSpan Cadence,
    TimeSpan Timeout,
    int LeastIdleSamples,
    int LeastLoadSamples,
    TimeSpan LeastLoadTime,
    int LeastDirectionSamples = 5,
    long BytesThatMeanTransfer = 1_000_000)
{
    public static BufferbloatSampling Default { get; } = new(
        IdleWindow: TimeSpan.FromSeconds(3),
        Cadence: TimeSpan.FromMilliseconds(250),
        Timeout: TimeSpan.FromSeconds(2),
        LeastIdleSamples: 5,
        LeastLoadSamples: 10,
        LeastLoadTime: TimeSpan.FromSeconds(3));
}
