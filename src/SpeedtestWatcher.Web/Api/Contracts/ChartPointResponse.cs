using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One point on the charts: a single test, or the average of the tests in a slice of the period. Readings are <c>null</c> when no test in it completed.
/// </summary>
public sealed record ChartPointResponse
{
    /// <summary>
    /// When the test ran, or the middle of the slice, in UTC.
    /// </summary>
    /// <example>2026-09-14T08:05:00Z</example>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Whether the test, or any test in the slice, failed.
    /// </summary>
    public required bool Failed { get; init; }

    /// <summary>
    /// Why the test failed, or how many tests in the slice failed.
    /// </summary>
    public required string? Error { get; init; }

    /// <summary>
    /// Ping in milliseconds.
    /// </summary>
    /// <example>12</example>
    public required int? Ping { get; init; }

    /// <summary>
    /// Jitter in milliseconds.
    /// </summary>
    /// <example>0.4</example>
    public required double? Jitter { get; init; }

    /// <summary>
    /// Download speed in Mbps.
    /// </summary>
    /// <example>941.25</example>
    public required double? Download { get; init; }

    /// <summary>
    /// Upload speed in Mbps.
    /// </summary>
    /// <example>110.5</example>
    public required double? Upload { get; init; }

    /// <summary>
    /// How long the test took, in seconds.
    /// </summary>
    /// <example>14</example>
    public required int? Duration { get; init; }

    public static ChartPointResponse From(ChartPoint point) => new()
    {
        Timestamp = point.Time,
        Failed = point.Failed,
        Error = point.Error,
        Ping = point.Ping,
        Jitter = point.Jitter,
        Download = point.Download,
        Upload = point.Upload,
        Duration = point.Duration
    };
}
