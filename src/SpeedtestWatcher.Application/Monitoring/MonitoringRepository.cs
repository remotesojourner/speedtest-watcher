using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Application.Monitoring;

internal class MonitoringRepository : IMonitoringRepository
{
    private const int MinutesInAnHour = 60;

    private readonly SpeedtestWatcherDbContext _db;

    public MonitoringRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task AddRoundAsync(ProbeRound round, CancellationToken cancellationToken = default)
    {
        _db.ProbeRounds.Add(round);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ProbeRound>> ListRoundsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _db.ProbeRounds
            .Where(round => round.At >= sinceUtc)
            .OrderBy(round => round.At)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LatencyPointDto>> LatencyAsync(DateTime fromUtc, DateTime toUtc, int slotMinutes, CancellationToken cancellationToken = default)
    {
        var slotHours = Math.Max(1, slotMinutes / MinutesInAnHour);
        var minutesInSlot = Math.Min(slotMinutes, MinutesInAnHour);
        var buckets = await _db.ProbeRounds
            .Where(round => round.At >= fromUtc && round.At <= toUtc)
            .GroupBy(round => new { round.At.Year, round.At.Month, round.At.Day, Hour = round.At.Hour / slotHours, Slot = round.At.Minute / minutesInSlot })
            .Select(rounds => new
            {
                rounds.Key,
                Milliseconds = rounds.Average(round => round.FastestMilliseconds),
                Rounds = rounds.Count(),
                Failed = rounds.Count(round => !round.Passed),
                DuringTest = rounds.Count(round => round.DuringTest)
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. buckets
                .Select(bucket => new LatencyPointDto(
                    new DateTime(bucket.Key.Year, bucket.Key.Month, bucket.Key.Day, bucket.Key.Hour * slotHours, bucket.Key.Slot * minutesInSlot, 0, DateTimeKind.Utc),
                    bucket.Milliseconds,
                    bucket.Rounds,
                    bucket.Failed,
                    bucket.DuringTest))
                .OrderBy(point => point.At)
        ];
    }

    public async Task<Outage> StartOutageAsync(DateTime startedAt, CancellationToken cancellationToken = default)
    {
        var outage = new Outage { StartedAt = startedAt };
        _db.Outages.Add(outage);
        await _db.SaveChangesAsync(cancellationToken);
        return outage;
    }

    public async Task<Outage?> EndOpenOutageAsync(DateTime endedAt, CancellationToken cancellationToken = default)
    {
        var outage = await GetOpenOutageAsync(cancellationToken);
        if (outage == null) return null;

        outage.EndedAt = endedAt;
        await _db.SaveChangesAsync(cancellationToken);
        return outage;
    }

    public async Task<Outage?> GetOpenOutageAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Outages
            .Where(outage => outage.EndedAt == null)
            .OrderByDescending(outage => outage.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Outage>> ListOutagesSinceAsync(DateTime sinceUtc, int limit, CancellationToken cancellationToken = default)
    {
        return await _db.Outages
            .Where(outage => outage.EndedAt == null || outage.EndedAt >= sinceUtc)
            .OrderByDescending(outage => outage.StartedAt)
            .Take(limit > 0 ? limit : 100)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteOutageAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Outages.Where(outage => outage.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task<WatchSession> StartWatchingAsync(DateTime at, CancellationToken cancellationToken = default)
    {
        var session = new WatchSession { StartedAt = at, LastSeenAt = at };
        _db.WatchSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<bool> KeepWatchingAsync(int sessionId, DateTime at, CancellationToken cancellationToken = default)
    {
        return await _db.WatchSessions
            .Where(session => session.Id == sessionId)
            .ExecuteUpdateAsync(session => session.SetProperty(s => s.LastSeenAt, at), cancellationToken) > 0;
    }

    public async Task<List<WatchSession>> ListWatchSessionsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _db.WatchSessions
            .Where(session => session.LastSeenAt >= sinceUtc)
            .OrderBy(session => session.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RemoveOldRoundsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0) return 0;

        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _db.ProbeRounds.Where(round => round.At <= cutoff).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> RemoveOldOutagesAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0) return 0;

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var removed = await _db.Outages
            .Where(outage => outage.EndedAt != null && outage.EndedAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        return removed + await _db.WatchSessions
            .Where(session => session.LastSeenAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
