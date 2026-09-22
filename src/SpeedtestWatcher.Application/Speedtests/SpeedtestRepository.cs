using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Application.Speedtests;

internal class SpeedtestRepository : ISpeedtestRepository
{
    private readonly SpeedtestWatcherDbContext _db;

    public SpeedtestRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(Speedtest test, CancellationToken cancellationToken = default)
    {
        _db.Speedtests.Add(test);
        await _db.SaveChangesAsync(cancellationToken);
        return test.Id;
    }

    public async Task<Speedtest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests.FindAsync([id], cancellationToken);
    }

    public async Task<Speedtest?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .OrderByDescending(t => t.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Speedtest?> GetLatestCompletedAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .Where(t => t.Status == TestStatus.Completed)
            .OrderByDescending(t => t.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Speedtest>> ListTestsAsync(int? afterId, int limit, TestStatus? status = null, TestType? type = null, bool? healthy = null, CancellationToken cancellationToken = default)
    {
        var query = Filter(status, type, healthy);
        if (afterId is { } cursorId && cursorId > 0)
        {
            var cursorCreated = await _db.Speedtests
                .Where(t => t.Id == cursorId)
                .Select(t => (DateTime?)t.Created)
                .SingleOrDefaultAsync(cancellationToken);
            if (cursorCreated is not { } created) return [];

            query = query.Where(t => t.Created < created || (t.Created == created && t.Id < cursorId));
        }

        return await query
            .OrderByDescending(t => t.Created)
            .ThenByDescending(t => t.Id)
            .Take(limit > 0 ? limit : 10)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Speedtest>> ListMatchingAsync(TestStatus? status, TestType? type, bool? healthy, IReadOnlyCollection<int>? ids = null, CancellationToken cancellationToken = default)
    {
        var query = Filter(status, type, healthy);
        if (ids != null)
        {
            query = query.Where(t => ids.Contains(t.Id));
        }

        return await query
            .OrderByDescending(t => t.Created)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountMatchingAsync(TestStatus? status, TestType? type, bool? healthy, CancellationToken cancellationToken = default) =>
        Filter(status, type, healthy).CountAsync(cancellationToken);

    private IQueryable<Speedtest> Filter(TestStatus? status, TestType? type, bool? healthy)
    {
        var query = _db.Speedtests.AsQueryable();

        if (status is { } wantedStatus)
        {
            query = query.Where(t => t.Status == wantedStatus);
        }

        if (type is { } wantedType)
        {
            query = query.Where(t => t.Type == wantedType);
        }

        if (healthy.HasValue)
        {
            query = query.Where(t => t.Healthy == healthy.Value);
        }

        return query;
    }

    public async Task<List<Speedtest>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .OrderByDescending(t => t.Created)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Speedtests.FindAsync([id], cancellationToken);
        if (entity == null) return false;

        _db.Speedtests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM speedtests", cancellationToken);
        return true;
    }

    public async Task<int> ImportTestsAsync(IEnumerable<Speedtest> tests, CancellationToken cancellationToken = default)
    {
        var incoming = tests.ToList();
        if (incoming.Count == 0) return 0;

        var earliest = incoming.Min(t => t.Created);
        var latest = incoming.Max(t => t.Created);
        var alreadyStored = (await _db.Speedtests
                .Where(t => t.Created >= earliest && t.Created <= latest)
                .Select(t => t.Created)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var count = 0;
        foreach (var test in incoming)
        {
            if (!alreadyStored.Add(test.Created)) continue;

            test.Id = 0;
            _db.Speedtests.Add(test);
            count++;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return count;
    }

    public async Task<int> RemoveOldTestsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        if (retentionDays <= 0) return 0;

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        return await _db.Speedtests
            .Where(t => t.Created <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests.CountAsync(cancellationToken);
    }

    public async Task<List<Speedtest>> ListCreatedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .Where(t => t.Created >= fromUtc && t.Created <= toUtc)
            .OrderBy(t => t.Created)
            .ToListAsync(cancellationToken);
    }
}
