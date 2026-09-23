using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One stretch of time the connection monitor saw the line down.
/// </summary>
public sealed record OutageResponse
{
    /// <summary>
    /// The outage's id.
    /// </summary>
    /// <example>7</example>
    public required int Id { get; init; }

    /// <summary>
    /// When the line went down, in UTC.
    /// </summary>
    /// <example>2026-09-14T20:35:00Z</example>
    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// When it came back, in UTC, or <c>null</c> while it is still down.
    /// </summary>
    /// <example>2026-09-14T20:41:30Z</example>
    public required DateTime? EndedAt { get; init; }

    /// <summary>
    /// How long it lasted in seconds. For an outage that hasn't ended, how long it has lasted so far.
    /// </summary>
    /// <example>390</example>
    public required long Seconds { get; init; }

    public static OutageResponse From(OutageDto outage) => new()
    {
        Id = outage.Id,
        StartedAt = outage.StartedAt,
        EndedAt = outage.EndedAt,
        Seconds = outage.Seconds ?? 0
    };
}
