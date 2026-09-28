using SpeedtestWatcher.Application.Models.Entities;

namespace SpeedtestWatcher.Application.Repositories.Interfaces;

public interface IRecommendationRepository
{
    Task<Recommendation?> GetAsync(CancellationToken cancellationToken = default);
    Task<Recommendation> SaveAsync(int ping, double download, double upload, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
