using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

internal sealed class DataBackupRepository : IDataBackupRepository
{
    private const int BatchSize = 5000;

    private readonly SpeedtestWatcherDbContext _db;

    public DataBackupRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public Task<List<Speedtest>> ListAllSpeedtestsAsync(CancellationToken cancellationToken = default) =>
        _db.Speedtests.OrderBy(test => test.Created).ToListAsync(cancellationToken);

    public Task<List<Outage>> ListAllOutagesAsync(CancellationToken cancellationToken = default) =>
        _db.Outages.OrderBy(outage => outage.StartedAt).ToListAsync(cancellationToken);

    public Task<List<WatchSession>> ListAllWatchSessionsAsync(CancellationToken cancellationToken = default) =>
        _db.WatchSessions.OrderBy(session => session.StartedAt).ToListAsync(cancellationToken);

    public Task<List<ProbeRound>> ListAllProbeRoundsAsync(CancellationToken cancellationToken = default) =>
        _db.ProbeRounds.OrderBy(round => round.At).ToListAsync(cancellationToken);

    public async Task<DataImportCounts> ImportAsync(
        List<Speedtest> speedtests,
        List<Outage> outages,
        List<WatchSession> watchSessions,
        List<ProbeRound> probeRounds,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var importedSpeedtests = await ImportSpeedtestsAsync(speedtests, cancellationToken);
        var importedOutages = await ImportOutagesAsync(outages, cancellationToken);
        var importedWatchSessions = await ImportWatchSessionsAsync(watchSessions, cancellationToken);
        var importedProbeRounds = await ImportProbeRoundsAsync(probeRounds, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new DataImportCounts(importedSpeedtests, importedOutages, importedWatchSessions, importedProbeRounds);
    }

    private async Task<int> ImportSpeedtestsAsync(List<Speedtest> incoming, CancellationToken cancellationToken)
    {
        if (incoming.Count == 0) return 0;

        var earliest = incoming.Min(test => test.Created);
        var latest = incoming.Max(test => test.Created);
        var alreadyStored = (await _db.Speedtests
                .Where(test => test.Created >= earliest && test.Created <= latest)
                .Select(test => test.Created)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var imported = 0;
        foreach (var batch in incoming.Chunk(BatchSize))
        {
            foreach (var test in batch)
            {
                if (!alreadyStored.Add(test.Created)) continue;

                test.Id = 0;
                _db.Speedtests.Add(test);
                imported++;
            }
            await _db.SaveChangesAsync(cancellationToken);
            _db.ChangeTracker.Clear();
        }
        return imported;
    }

    private async Task<int> ImportOutagesAsync(List<Outage> incoming, CancellationToken cancellationToken)
    {
        if (incoming.Count == 0) return 0;

        var earliest = incoming.Min(outage => outage.StartedAt);
        var latest = incoming.Max(outage => outage.StartedAt);
        var alreadyStored = (await _db.Outages
                .Where(outage => outage.StartedAt >= earliest && outage.StartedAt <= latest)
                .Select(outage => outage.StartedAt)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var imported = 0;
        foreach (var batch in incoming.Chunk(BatchSize))
        {
            foreach (var outage in batch)
            {
                if (!alreadyStored.Add(outage.StartedAt)) continue;

                outage.Id = 0;
                _db.Outages.Add(outage);
                imported++;
            }
            await _db.SaveChangesAsync(cancellationToken);
            _db.ChangeTracker.Clear();
        }
        return imported;
    }

    private async Task<int> ImportWatchSessionsAsync(List<WatchSession> incoming, CancellationToken cancellationToken)
    {
        if (incoming.Count == 0) return 0;

        var earliest = incoming.Min(session => session.StartedAt);
        var latest = incoming.Max(session => session.StartedAt);
        var alreadyStored = (await _db.WatchSessions
                .Where(session => session.StartedAt >= earliest && session.StartedAt <= latest)
                .Select(session => session.StartedAt)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var imported = 0;
        foreach (var batch in incoming.Chunk(BatchSize))
        {
            foreach (var session in batch)
            {
                if (!alreadyStored.Add(session.StartedAt)) continue;

                session.Id = 0;
                _db.WatchSessions.Add(session);
                imported++;
            }
            await _db.SaveChangesAsync(cancellationToken);
            _db.ChangeTracker.Clear();
        }
        return imported;
    }

    private async Task<int> ImportProbeRoundsAsync(List<ProbeRound> incoming, CancellationToken cancellationToken)
    {
        if (incoming.Count == 0) return 0;

        var earliest = incoming.Min(round => round.At);
        var latest = incoming.Max(round => round.At);
        var alreadyStored = (await _db.ProbeRounds
                .Where(round => round.At >= earliest && round.At <= latest)
                .Select(round => round.At)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var imported = 0;
        foreach (var batch in incoming.Chunk(BatchSize))
        {
            foreach (var round in batch)
            {
                if (!alreadyStored.Add(round.At)) continue;

                round.Id = 0;
                _db.ProbeRounds.Add(round);
                imported++;
            }
            await _db.SaveChangesAsync(cancellationToken);
            _db.ChangeTracker.Clear();
        }
        return imported;
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.ProbeRounds.ExecuteDeleteAsync(cancellationToken);
        await _db.WatchSessions.ExecuteDeleteAsync(cancellationToken);
        await _db.Outages.ExecuteDeleteAsync(cancellationToken);
        await _db.Speedtests.ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
