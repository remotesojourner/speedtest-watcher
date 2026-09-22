using SpeedtestWatcher.Application.Statistics;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// How much a speed varied.
/// </summary>
public sealed record SpeedConsistencyResponse
{
    /// <summary>
    /// The standard deviation, in Mbps.
    /// </summary>
    /// <example>164.38</example>
    public required double StdDev { get; init; }

    /// <summary>
    /// 100 minus the standard deviation as a percentage of the average, never below 0. 100 means every test got the same speed.
    /// </summary>
    /// <example>78.8</example>
    public required double Consistency { get; init; }

    public static SpeedConsistencyResponse From(ConsistencyItemDto item) => new() { StdDev = item.StdDev, Consistency = item.Consistency };
}
