using System.Collections.Concurrent;

namespace SpeedtestWatcher.Application.Integrations;

internal sealed class HeartbeatSchedule
{
    private const long EarlyToleranceMilliseconds = 30 * 1000;

    private readonly ConcurrentDictionary<string, long> _lastSent = new();

    public bool IsDue(string integrationId, int intervalMinutes)
    {
        if (intervalMinutes <= 1) return true;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_lastSent.TryGetValue(integrationId, out var last) && now - last < intervalMinutes * 60_000L - EarlyToleranceMilliseconds)
            return false;

        _lastSent[integrationId] = now;
        return true;
    }
}
