namespace SpeedtestWatcher.Application.Models.Dtos;

public sealed record LatencyPointDto(DateTime At, double? Milliseconds, int Rounds, int Failed, int DuringTest);
