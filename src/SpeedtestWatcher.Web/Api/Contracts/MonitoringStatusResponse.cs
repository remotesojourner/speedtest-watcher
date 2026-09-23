using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// What the connection monitor sees right now, with uptime for the usual periods.
/// </summary>
public sealed record MonitoringStatusResponse
{
    /// <summary>
    /// The line as the monitor last saw it: <c>up</c>, <c>down</c>, or <c>unknown</c> before it has enough rounds.
    /// </summary>
    /// <example>up</example>
    public required string State { get; init; }

    /// <summary>
    /// Whether the monitor is watching at all. It is off when monitoring is switched off or no targets are set.
    /// </summary>
    /// <example>true</example>
    public required bool Watching { get; init; }

    /// <summary>
    /// Since when the line has been in this state, in UTC.
    /// </summary>
    /// <example>2026-09-14T20:41:30Z</example>
    public required DateTime? Since { get; init; }

    /// <summary>
    /// When the last probe round ran, in UTC.
    /// </summary>
    /// <example>2026-09-14T20:42:00Z</example>
    public required DateTime? LastRoundAt { get; init; }

    /// <summary>
    /// The quickest connect time of the last round, in milliseconds.
    /// </summary>
    /// <example>12.4</example>
    public required double? FastestMilliseconds { get; init; }

    /// <summary>
    /// The outage in progress, when the line is down.
    /// </summary>
    public required OutageResponse? CurrentOutage { get; init; }

    /// <summary>
    /// Uptime over the last 24 hours.
    /// </summary>
    public required UptimeResponse Last24Hours { get; init; }

    /// <summary>
    /// Uptime over the last 7 days.
    /// </summary>
    public required UptimeResponse Last7Days { get; init; }

    /// <summary>
    /// Uptime over the last 30 days.
    /// </summary>
    public required UptimeResponse Last30Days { get; init; }

    public static MonitoringStatusResponse From(MonitoringStatusDto status) => new()
    {
        State = status.Health.ToName(),
        Watching = status.Watching,
        Since = status.Since,
        LastRoundAt = status.LastRoundAt,
        FastestMilliseconds = status.FastestMilliseconds,
        CurrentOutage = status.CurrentOutage is { } outage ? OutageResponse.From(outage) : null,
        Last24Hours = UptimeResponse.From(status.Last24Hours),
        Last7Days = UptimeResponse.From(status.Last7Days),
        Last30Days = UptimeResponse.From(status.Last30Days)
    };
}
