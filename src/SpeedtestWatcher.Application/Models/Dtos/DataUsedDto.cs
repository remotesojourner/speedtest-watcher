namespace SpeedtestWatcher.Application.Models.Dtos;

public sealed record DataUsedDto(long Last24Hours, long Last7Days, long Last30Days, long Stored);
