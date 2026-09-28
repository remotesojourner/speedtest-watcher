using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Entities;

namespace SpeedtestWatcher.Application.Repositories.Interfaces;

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
