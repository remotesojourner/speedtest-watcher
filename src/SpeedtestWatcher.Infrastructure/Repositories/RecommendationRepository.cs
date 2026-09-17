using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    public const int CompletedTestsNeeded = 10;

    private const int PlaceholderPing = 25;
    private const double PlaceholderDownload = 100;
    private const double PlaceholderUpload = 50;

    private readonly SpeedtestWatcherDbContext _db;

    public RecommendationRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<Recommendation?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Recommendations.OrderBy(r => r.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Recommendation?> RecalculateAsync(CancellationToken cancellationToken = default)
    {
        var recentTests = await _db.Speedtests
            .Where(t => t.Status == "completed")
            .OrderByDescending(t => t.Created)
            .Take(CompletedTestsNeeded)
            .ToListAsync(cancellationToken);

        if (recentTests.Count < CompletedTestsNeeded) return null;

        var ping = recentTests.Min(t => t.Ping);
        var download = Math.Round(recentTests.Max(t => t.Download), 2);
        var upload = Math.Round(recentTests.Max(t => t.Upload), 2);

        var recommendation = await GetAsync(cancellationToken);
        if (recommendation != null && recommendation.Ping == ping && recommendation.Download == download && recommendation.Upload == upload)
            return null;

        if (recommendation == null)
        {
            recommendation = new Recommendation();
            _db.Recommendations.Add(recommendation);
        }

        recommendation.Ping = ping;
        recommendation.Download = download;
        recommendation.Upload = upload;
        await _db.SaveChangesAsync(cancellationToken);
        return recommendation;
    }

    public async Task RemovePlaceholderAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Speedtests.CountAsync(t => t.Status == "completed", cancellationToken) >= CompletedTestsNeeded) return;

        await _db.Recommendations
            .Where(r => r.Ping == PlaceholderPing && r.Download == PlaceholderDownload && r.Upload == PlaceholderUpload)
            .ExecuteDeleteAsync(cancellationToken);
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
