namespace SpeedtestWatcher.Application.Models.Dtos;

public sealed record UptimeDayDto(DateOnly Date, int Outages, long DownSeconds);
