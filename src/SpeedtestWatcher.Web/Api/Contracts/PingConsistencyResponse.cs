namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// How much ping varied.
/// </summary>
public sealed record PingConsistencyResponse
{
    /// <summary>
    /// The standard deviation of ping, in milliseconds.
    /// </summary>
    /// <example>9.5</example>
    public required double StdDev { get; init; }

    /// <summary>
    /// The same standard deviation, kept for older dashboards.
    /// </summary>
    /// <example>9.5</example>
    public required double Jitter { get; init; }
}
