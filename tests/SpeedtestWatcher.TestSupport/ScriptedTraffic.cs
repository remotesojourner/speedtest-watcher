using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.TestSupport;

public sealed class ScriptedTraffic : INetworkTraffic
{
    private const long ChunkPerRead = 50_000_000;

    private long _received;
    private long _sent;

    public LoadDirection Moving { get; set; } = LoadDirection.Unclear;

    public TrafficCounters Read()
    {
        switch (Moving)
        {
            case LoadDirection.Download:
                Interlocked.Add(ref _received, ChunkPerRead);
                break;
            case LoadDirection.Upload:
                Interlocked.Add(ref _sent, ChunkPerRead);
                break;
            default:
                break;
        }

        return new TrafficCounters(Interlocked.Read(ref _received), Interlocked.Read(ref _sent));
    }
}
