using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    private readonly SpeedtestWatcherDbContext _db;

    public RecommendationRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<Recommendation?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Recommendations.OrderBy(r => r.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Recommendation> UpdateOrCalculateAsync(CancellationToken cancellationToken = default)
    {
        var recentTests = await _db.Speedtests
            .Where(t => t.Status == "completed")
            .OrderByDescending(t => t.Created)
            .Take(10)
            .ToListAsync(cancellationToken);

        var existing = await GetAsync(cancellationToken);

        if (recentTests.Count >= 10)
        {
            var minPing = recentTests.Min(t => t.Ping);
            var maxDown = Math.Round(recentTests.Max(t => t.Download), 2);
            var maxUp = Math.Round(recentTests.Max(t => t.Upload), 2);

            if (existing == null)
            {
                existing = new Recommendation
                {
                    Ping = minPing,
                    Download = maxDown,
                    Upload = maxUp
                };
                _db.Recommendations.Add(existing);
            }
            else
            {
                existing.Ping = minPing;
                existing.Download = maxDown;
                existing.Upload = maxUp;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        if (existing == null)
        {
            existing = new Recommendation
            {
                Ping = 25,
                Download = 100,
                Upload = 50
            };
            _db.Recommendations.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    public async Task SaveAsync(int ping, double download, double upload, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(cancellationToken);
        if (existing == null)
        {
            _db.Recommendations.Add(new Recommendation { Ping = ping, Download = download, Upload = upload });
        }
        else
        {
            existing.Ping = ping;
            existing.Download = download;
            existing.Upload = upload;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM recommendations", cancellationToken);
    }
}
