using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Application.Recommendations;

internal class RecommendationRepository : IRecommendationRepository
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

    public async Task<Recommendation> SaveAsync(int ping, double download, double upload, CancellationToken cancellationToken = default)
    {
        var recommendation = await GetAsync(cancellationToken);
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

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM recommendations", cancellationToken);
    }
}
