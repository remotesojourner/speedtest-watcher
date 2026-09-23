namespace SpeedtestWatcher.Application.Monitoring;

public sealed record OutageDto(int Id, DateTime StartedAt, DateTime? EndedAt, long? Seconds);

public sealed record UptimeDto(double? Percent, long WatchedSeconds, long DownSeconds, int Outages);

public sealed record UptimeDayDto(DateOnly Date, int Outages, long DownSeconds);

public sealed record MonitoringStatusDto(
    ConnectionHealth Health,
    bool Watching,
    DateTime? Since,
    DateTime? LastRoundAt,
    double? FastestMilliseconds,
    OutageDto? CurrentOutage,
    UptimeDto Last24Hours,
    UptimeDto Last7Days,
    UptimeDto Last30Days);
