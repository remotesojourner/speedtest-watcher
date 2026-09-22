using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Averages of the completed tests that ran in one hour of the day.
/// </summary>
public sealed record HourlyAverageResponse
{
    /// <summary>
    /// The hour of the day, 0 to 23, in the requested time zone.
    /// </summary>
    /// <example>16</example>
    public required int Hour { get; init; }

    /// <summary>
    /// How many completed tests ran in this hour.
    /// </summary>
    /// <example>1</example>
    public required int Count { get; init; }

    /// <summary>
    /// Average download speed in Mbps, or <c>null</c> when no test ran in this hour.
    /// </summary>
    /// <example>612.5</example>
    public required double? Download { get; init; }

    /// <summary>
    /// Average upload speed in Mbps, or <c>null</c> when no test ran in this hour.
    /// </summary>
    /// <example>98.12</example>
    public required double? Upload { get; init; }

    /// <summary>
    /// Average ping in milliseconds, or <c>null</c> when no test ran in this hour.
    /// </summary>
    /// <example>31</example>
    public required int? Ping { get; init; }

    /// <summary>
    /// Average jitter in milliseconds, or <c>null</c> when no test in this hour reported it.
    /// </summary>
    /// <example>2.75</example>
    public required double? Jitter { get; init; }

    public static HourlyAverageResponse From(HourlyAverageDto hour) => new()
    {
        Hour = hour.Hour,
        Count = hour.Count,
        Download = hour.Download,
        Upload = hour.Upload,
        Ping = hour.Ping,
        Jitter = hour.Jitter
    };
}
