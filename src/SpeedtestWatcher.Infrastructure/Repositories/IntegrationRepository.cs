using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class IntegrationRepository : IIntegrationRepository
{
    private readonly SpeedtestWatcherDbContext _db;

    public IntegrationRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<List<IntegrationData>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Integrations.ToListAsync(cancellationToken);
    }

    public async Task<IntegrationData?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _db.Integrations.FindAsync([id], cancellationToken);
    }

    public async Task<string> CreateAsync(string name, string displayName, string dataJson, CancellationToken cancellationToken = default)
    {
        var integration = new IntegrationData
        {
            Id = NewId(),
            Name = name,
            DisplayName = DisplayNameOrDefault(displayName),
            Data = dataJson
        };

        _db.Integrations.Add(integration);
        await _db.SaveChangesAsync(cancellationToken);
        return integration.Id;
    }

    public async Task UpsertAsync(IntegrationData integration, CancellationToken cancellationToken = default)
    {
        var id = string.IsNullOrWhiteSpace(integration.Id) ? NewId() : integration.Id;
        var existing = await _db.Integrations.FindAsync([id], cancellationToken);
        if (existing == null)
        {
            _db.Integrations.Add(new IntegrationData
            {
                Id = id,
                Name = integration.Name,
                DisplayName = DisplayNameOrDefault(integration.DisplayName),
                Data = integration.Data
            });
        }
        else
        {
            existing.Name = integration.Name;
            existing.DisplayName = DisplayNameOrDefault(integration.DisplayName);
            existing.Data = integration.Data;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    private static string DisplayNameOrDefault(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? "Untitled" : displayName;

    public async Task<bool> PatchAsync(string id, string? displayName, string dataJson, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Integrations.FindAsync([id], cancellationToken);
        if (entity == null) return false;

        if (!string.IsNullOrWhiteSpace(displayName))
            entity.DisplayName = displayName;

        entity.Data = dataJson;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Integrations.FindAsync([id], cancellationToken);
        if (entity == null) return false;

        _db.Integrations.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateActivityAsync(string id, bool error, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Integrations.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            entity.LastActivity = DateTime.UtcNow;
            entity.ActivityFailed = error;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM integration_data", cancellationToken);
    }
}
