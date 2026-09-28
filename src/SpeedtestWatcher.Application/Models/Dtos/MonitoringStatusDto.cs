using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Models.Dtos;

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
