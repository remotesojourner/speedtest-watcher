namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// The period statistics cover.
/// </summary>
public sealed record StatisticsPeriodResponse
{
    /// <summary>
    /// The start, as requested, or seven days ago.
    /// </summary>
    /// <example>2026-09-14</example>
    public required string From { get; init; }

    /// <summary>
    /// The end, as requested, or today.
    /// </summary>
    /// <example>2026-09-15</example>
    public required string To { get; init; }

    /// <summary>
    /// How many days the period spans, rounded up.
    /// </summary>
    /// <example>2</example>
    public required int Days { get; init; }
}
