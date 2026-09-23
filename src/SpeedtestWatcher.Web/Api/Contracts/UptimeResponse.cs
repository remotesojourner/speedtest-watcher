using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Uptime over a period. Only time the monitor was watching counts, so a restart is neither uptime nor downtime.
/// </summary>
public sealed record UptimeResponse
{
    /// <summary>
    /// The share of the watched time the line was up, as a percentage, or <c>null</c> when nothing was watched.
    /// </summary>
    /// <example>99.86</example>
    public required double? Percent { get; init; }

    /// <summary>
    /// How many seconds of this period the monitor was watching.
    /// </summary>
    /// <example>86400</example>
    public required long WatchedSeconds { get; init; }

    /// <summary>
    /// How many of those seconds the line was down.
    /// </summary>
    /// <example>120</example>
    public required long DownSeconds { get; init; }

    /// <summary>
    /// How many outages fall in this period.
    /// </summary>
    /// <example>1</example>
    public required int Outages { get; init; }

    public static UptimeResponse From(UptimeDto uptime) => new()
    {
        Percent = uptime.Percent,
        WatchedSeconds = uptime.WatchedSeconds,
        DownSeconds = uptime.DownSeconds,
        Outages = uptime.Outages
    };
}
