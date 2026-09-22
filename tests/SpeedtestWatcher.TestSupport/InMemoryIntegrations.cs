using SpeedtestWatcher.Application.Integrations;

namespace SpeedtestWatcher.TestSupport;

internal sealed class InMemoryIntegrations(List<IntegrationData> items) : IIntegrationRepository
{
    public List<bool> ActivityErrors { get; } = [];

    public Task<List<IntegrationData>> ListAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(items);

    public Task UpdateActivityAsync(string id, bool error, CancellationToken cancellationToken = default)
    {
        ActivityErrors.Add(error);
        return Task.CompletedTask;
    }

    public Task<IntegrationData?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string> CreateAsync(string name, string displayName, string dataJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> PatchAsync(string id, string? displayName, string dataJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpsertAsync(IntegrationData integration, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task ClearAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
