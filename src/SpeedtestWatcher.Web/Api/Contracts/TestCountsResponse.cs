namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// How many tests ran in a period.
/// </summary>
public sealed record TestCountsResponse
{
    /// <summary>
    /// Every test in the period, including failed and skipped ones.
    /// </summary>
    /// <example>4</example>
    public required int Total { get; init; }

    /// <summary>
    /// The tests that failed.
    /// </summary>
    /// <example>1</example>
    public required int Failed { get; init; }
}
