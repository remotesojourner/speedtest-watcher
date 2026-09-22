namespace SpeedtestWatcher.Application.Integrations;

public interface IIntegrationRepository
{
    Task<List<IntegrationData>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<IntegrationData?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<string> CreateAsync(string name, string displayName, string dataJson, CancellationToken cancellationToken = default);
    Task<bool> PatchAsync(string id, string? displayName, string dataJson, CancellationToken cancellationToken = default);
    Task UpsertAsync(IntegrationData integration, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateActivityAsync(string id, bool error, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
