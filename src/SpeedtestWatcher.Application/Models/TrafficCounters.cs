namespace SpeedtestWatcher.Application.Models;

public readonly record struct TrafficCounters(long Received, long Sent)
{
    public static TrafficCounters None { get; }

    public TrafficCounters Since(TrafficCounters earlier) => new(Received - earlier.Received, Sent - earlier.Sent);
}
