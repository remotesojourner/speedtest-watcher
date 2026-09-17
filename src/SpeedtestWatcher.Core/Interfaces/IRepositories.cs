using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Interfaces;

public interface IConfigRepository
{
    Task<List<ConfigEntry>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> UpdateValueAsync(string key, string value, CancellationToken cancellationToken = default);
    Task InsertDefaultsAsync(CancellationToken cancellationToken = default);
    Task<string?> ValidateInputAsync(string key, object? value, CancellationToken cancellationToken = default);
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}

public interface IIntegrationRepository
{
    Task<List<IntegrationData>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<List<IntegrationData>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IntegrationData?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<string> CreateAsync(string name, string displayName, string dataJson, CancellationToken cancellationToken = default);
    Task<bool> PatchAsync(string id, string? displayName, string dataJson, CancellationToken cancellationToken = default);
    Task UpsertAsync(IntegrationData integration, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateActivityAsync(string id, bool error, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}

public interface IStorageRepository
{
    Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default);
}

public interface IRecommendationRepository
{
    Task<Recommendation?> GetAsync(CancellationToken cancellationToken = default);
    Task<Recommendation?> RecalculateAsync(CancellationToken cancellationToken = default);
    Task RemovePlaceholderAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(int ping, double download, double upload, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
