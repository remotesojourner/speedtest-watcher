using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal static class TestSampling
{
    public static BufferbloatSampling Bufferbloat { get; } = new(
        IdleWindow: TimeSpan.FromMilliseconds(250),
        Cadence: TimeSpan.FromMilliseconds(5),
        Timeout: TimeSpan.FromMilliseconds(100),
        LeastIdleSamples: 5,
        LeastLoadSamples: 10,
        LeastLoadTime: TimeSpan.FromMilliseconds(100));
}
