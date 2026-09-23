using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One day of the uptime calendar.
/// </summary>
public sealed record UptimeDayResponse
{
    /// <summary>
    /// The day, in the time zone the request asked for.
    /// </summary>
    /// <example>2026-09-14</example>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// How many outages touched this day.
    /// </summary>
    /// <example>2</example>
    public required int Outages { get; init; }

    /// <summary>
    /// How many seconds of this day the line was down.
    /// </summary>
    /// <example>420</example>
    public required long DownSeconds { get; init; }

    public static UptimeDayResponse From(UptimeDayDto day) => new()
    {
        Date = day.Date,
        Outages = day.Outages,
        DownSeconds = day.DownSeconds
    };
}
