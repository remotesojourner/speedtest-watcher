using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// A full data backup: every result, and the connection monitoring history, in the same format as the file the Storage tab exports.
/// </summary>
public sealed record DataBackup
{
    /// <summary>
    /// The format version. An import is rejected when this is newer than the running version knows.
    /// </summary>
    /// <example>1</example>
    public int Version { get; init; } = DataBackupDto.CurrentVersion;

    /// <summary>
    /// When the backup was made.
    /// </summary>
    public DateTime Exported { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Every result. Required: a backup with nothing in any of its four lists is rejected.
    /// </summary>
    public IReadOnlyList<DataBackupSpeedtest> Speedtests { get; init; } = [];

    /// <summary>
    /// Every connection outage.
    /// </summary>
    public IReadOnlyList<DataBackupOutage> Outages { get; init; } = [];

    /// <summary>
    /// Every period the connection monitor was watching.
    /// </summary>
    public IReadOnlyList<DataBackupWatchSession> WatchSessions { get; init; } = [];

    /// <summary>
    /// Every connection check behind the latency chart, kept for 30 days.
    /// </summary>
    public IReadOnlyList<DataBackupProbeRound> ProbeRounds { get; init; } = [];

    public static DataBackup From(DataBackupDto backup) => new()
    {
        Version = backup.Version,
        Exported = backup.Exported,
        Speedtests = backup.Speedtests.Select(DataBackupSpeedtest.From).ToList(),
        Outages = backup.Outages.Select(DataBackupOutage.From).ToList(),
        WatchSessions = backup.WatchSessions.Select(DataBackupWatchSession.From).ToList(),
        ProbeRounds = backup.ProbeRounds.Select(DataBackupProbeRound.From).ToList()
    };

    public DataBackupDto ToDto() => new()
    {
        Version = Version,
        Exported = Exported,
        Speedtests = Speedtests.Select(speedtest => speedtest.ToImportRow()).ToList(),
        Outages = Outages.Select(outage => outage.ToRow()).ToList(),
        WatchSessions = WatchSessions.Select(session => session.ToRow()).ToList(),
        ProbeRounds = ProbeRounds.Select(round => round.ToRow()).ToList()
    };
}
