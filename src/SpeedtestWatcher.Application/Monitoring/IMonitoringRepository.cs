namespace SpeedtestWatcher.Application.Monitoring;

public interface IMonitoringRepository
{
    Task AddRoundAsync(ProbeRound round, CancellationToken cancellationToken = default);
    Task<List<ProbeRound>> ListRoundsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<List<LatencyPointDto>> LatencyAsync(DateTime fromUtc, DateTime toUtc, int slotMinutes, CancellationToken cancellationToken = default);

    Task<Outage> StartOutageAsync(DateTime startedAt, CancellationToken cancellationToken = default);
    Task<Outage?> EndOpenOutageAsync(DateTime endedAt, CancellationToken cancellationToken = default);
    Task<Outage?> GetOpenOutageAsync(CancellationToken cancellationToken = default);
    Task<List<Outage>> ListOutagesSinceAsync(DateTime sinceUtc, int limit, CancellationToken cancellationToken = default);
    Task<bool> DeleteOutageAsync(int id, CancellationToken cancellationToken = default);

    Task<WatchSession> StartWatchingAsync(DateTime at, CancellationToken cancellationToken = default);
    Task<bool> KeepWatchingAsync(int sessionId, DateTime at, CancellationToken cancellationToken = default);
    Task<List<WatchSession>> ListWatchSessionsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<int> RemoveOldRoundsAsync(int days, CancellationToken cancellationToken = default);
    Task<int> RemoveOldOutagesAsync(int days, CancellationToken cancellationToken = default);
}
