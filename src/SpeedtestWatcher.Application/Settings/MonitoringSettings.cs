using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Application.Settings;

public sealed record MonitoringSettings(
    bool Enabled,
    IReadOnlyList<ProbeTarget> Targets,
    int IntervalSeconds,
    int RoundsToGoDown,
    int RoundsToGoUp,
    bool TestAfterReconnect)
{
    public const string DefaultTargets = "1.1.1.1:443,8.8.8.8:443,9.9.9.9:443";

    public const int ShortestInterval = 5;
    public const int LongestInterval = 3600;
    public const int MostRounds = 10;

    public static TimeSpan ProbeTimeout { get; } = TimeSpan.FromSeconds(2);

    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);

    public bool Watching => Enabled && Targets.Count > 0;
}
