namespace SpeedtestWatcher.Application.Models.Dtos;

public sealed record UptimeDto(double? Percent, long WatchedSeconds, long DownSeconds, int Outages);
