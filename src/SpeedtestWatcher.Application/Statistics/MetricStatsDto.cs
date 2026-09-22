namespace SpeedtestWatcher.Application.Statistics;

public class MetricStatsDto<T>
{
    public T Min { get; set; } = default!;
    public T Max { get; set; } = default!;
    public T Avg { get; set; } = default!;
}
