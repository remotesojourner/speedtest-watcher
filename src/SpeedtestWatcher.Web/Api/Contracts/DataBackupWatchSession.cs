using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One period the connection monitor was watching, in a data backup. Uptime is worked out from these together with the outages.
/// </summary>
public sealed record DataBackupWatchSession
{
    /// <summary>
    /// When the monitor started watching.
    /// </summary>
    /// <example>2026-09-14T08:00:00Z</example>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// The last time the monitor was seen watching. A session that ends before it starts is skipped on import.
    /// </summary>
    /// <example>2026-09-14T08:15:00Z</example>
    public DateTime LastSeenAt { get; init; }

    public static DataBackupWatchSession From(DataBackupWatchSessionRow row) => new() { StartedAt = row.StartedAt, LastSeenAt = row.LastSeenAt };

    public DataBackupWatchSessionRow ToRow() => new() { StartedAt = StartedAt, LastSeenAt = LastSeenAt };
}
