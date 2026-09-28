namespace SpeedtestWatcher.Application.Models.Dtos;

public sealed record OutageDto(int Id, DateTime StartedAt, DateTime? EndedAt, long? Seconds);
