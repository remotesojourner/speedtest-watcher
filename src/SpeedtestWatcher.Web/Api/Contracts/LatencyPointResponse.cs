using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One point of the latency chart: the probe rounds of a slot, averaged.
/// </summary>
public sealed record LatencyPointResponse
{
    /// <summary>
    /// The start of the slot, in UTC.
    /// </summary>
    /// <example>2026-09-14T20:35:00Z</example>
    public required DateTime At { get; init; }

    /// <summary>
    /// The average of the quickest connect time of each round in the slot, in milliseconds, or <c>null</c> when no round answered.
    /// </summary>
    /// <example>12.4</example>
    public required double? Milliseconds { get; init; }

    /// <summary>
    /// How many rounds fall in this slot.
    /// </summary>
    /// <example>20</example>
    public required int Rounds { get; init; }

    /// <summary>
    /// How many of them failed.
    /// </summary>
    /// <example>2</example>
    public required int Failed { get; init; }

    /// <summary>
    /// How many of them ran while a speedtest was in progress. Those rounds never start an outage.
    /// </summary>
    /// <example>4</example>
    public required int DuringTest { get; init; }

    public static LatencyPointResponse From(LatencyPointDto point) => new()
    {
        At = point.At,
        Milliseconds = point.Milliseconds,
        Rounds = point.Rounds,
        Failed = point.Failed,
        DuringTest = point.DuringTest
    };
}
