using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One connection outage in a data backup.
/// </summary>
public sealed record DataBackupOutage
{
    /// <summary>
    /// When the outage started.
    /// </summary>
    /// <example>2026-09-14T08:05:00Z</example>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the outage ended. An outage still open when the backup was made, or one that ends before it starts, is skipped on import.
    /// </summary>
    /// <example>2026-09-14T08:11:00Z</example>
    public DateTime? EndedAt { get; init; }

    public static DataBackupOutage From(DataBackupOutageRow row) => new() { StartedAt = row.StartedAt, EndedAt = row.EndedAt };

    public DataBackupOutageRow ToRow() => new() { StartedAt = StartedAt, EndedAt = EndedAt };
}
