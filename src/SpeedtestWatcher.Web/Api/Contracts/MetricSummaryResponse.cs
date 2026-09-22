using SpeedtestWatcher.Application.Statistics;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// The lowest, average and highest value of one reading.
/// </summary>
public sealed record MetricSummaryResponse
{
    /// <summary>
    /// The lowest value.
    /// </summary>
    /// <example>612.5</example>
    public required double Min { get; init; }

    /// <summary>
    /// The average, rounded to two decimals. Ping and time averages are rounded to whole numbers.
    /// </summary>
    /// <example>776.88</example>
    public required double Avg { get; init; }

    /// <summary>
    /// The highest value.
    /// </summary>
    /// <example>941.25</example>
    public required double Max { get; init; }

    public static MetricSummaryResponse? From(MetricStatsDto<int>? stats) =>
        stats == null ? null : new() { Min = stats.Min, Avg = stats.Avg, Max = stats.Max };

    public static MetricSummaryResponse? From(MetricStatsDto<double>? stats) =>
        stats == null ? null : new() { Min = stats.Min, Avg = stats.Avg, Max = stats.Max };
}
