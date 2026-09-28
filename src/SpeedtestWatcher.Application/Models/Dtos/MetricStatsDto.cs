namespace SpeedtestWatcher.Application.Models.Dtos;

public class MetricStatsDto<T>
{
    public T Min { get; set; } = default!;
    public T Max { get; set; } = default!;
    public T Avg { get; set; } = default!;
}
