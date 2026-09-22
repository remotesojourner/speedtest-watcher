namespace SpeedtestWatcher.Application.Statistics;

public sealed record SpeedtestStatistics(
    TestsCountDto Tests,
    MetricStatsDto<int>? Ping,
    MetricStatsDto<double>? Jitter,
    MetricStatsDto<double>? Download,
    MetricStatsDto<double>? Upload,
    MetricStatsDto<int>? Time,
    IReadOnlyList<ChartPoint> ChartPoints,
    IReadOnlyList<HourlyAverageDto> HourlyAverages,
    ConsistencyDto Consistency,
    int RawDataPoints,
    bool Downsampled,
    DateRangeDto DateRange);
