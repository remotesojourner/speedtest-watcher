using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// What a data backup import restored.
/// </summary>
public sealed record DataImportResponse
{
    /// <summary>
    /// Results that were stored.
    /// </summary>
    /// <example>1204</example>
    public required int Speedtests { get; init; }

    /// <summary>
    /// Outages that were stored.
    /// </summary>
    /// <example>6</example>
    public required int Outages { get; init; }

    /// <summary>
    /// Watch sessions that were stored.
    /// </summary>
    /// <example>40</example>
    public required int WatchSessions { get; init; }

    /// <summary>
    /// Probe rounds that were stored.
    /// </summary>
    /// <example>172800</example>
    public required int ProbeRounds { get; init; }

    /// <summary>
    /// Entries that were already stored, or left out because they weren't valid, such as an outage still open when the backup was made.
    /// </summary>
    /// <example>0</example>
    public required int Skipped { get; init; }

    public static DataImportResponse From(DataImportResultDto result) => new()
    {
        Speedtests = result.Speedtests,
        Outages = result.Outages,
        WatchSessions = result.WatchSessions,
        ProbeRounds = result.ProbeRounds,
        Skipped = result.Skipped
    };
}
