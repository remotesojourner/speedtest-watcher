using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

public interface IDataBackupRepository
{
    Task<List<Speedtest>> ListAllSpeedtestsAsync(CancellationToken cancellationToken = default);
    Task<List<Outage>> ListAllOutagesAsync(CancellationToken cancellationToken = default);
    Task<List<WatchSession>> ListAllWatchSessionsAsync(CancellationToken cancellationToken = default);
    Task<List<ProbeRound>> ListAllProbeRoundsAsync(CancellationToken cancellationToken = default);

    Task<DataImportCounts> ImportAsync(
        List<Speedtest> speedtests,
        List<Outage> outages,
        List<WatchSession> watchSessions,
        List<ProbeRound> probeRounds,
        CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}

public sealed record DataImportCounts(int Speedtests, int Outages, int WatchSessions, int ProbeRounds);
