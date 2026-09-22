using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Core.Settings;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class SettingsStore : ISettingsStore
{
    private readonly SpeedtestWatcherDbContext _db;

    public SettingsStore(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) =>
        AppSettings.From(await GetValuesAsync(cancellationToken));

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _db.Configs.AsNoTracking().ToDictionaryAsync(entry => entry.Key, entry => entry.Value, cancellationToken);
        return SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => stored.GetValueOrDefault(definition.Key, definition.Default));
    }

    public Task<SettingsSaveResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default) =>
        WriteAsync(changes, definition => !definition.IsManagedOnSecurityTab, "Sign-in settings are changed on the Security tab", cancellationToken);

    public Task<SettingsSaveResult> SaveSignInAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default) =>
        WriteAsync(changes, definition => definition.IsManagedOnSecurityTab, "Only sign-in settings are changed on the Security tab", cancellationToken);

    public async Task InsertDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var obsolete = await _db.Configs.Where(c => SettingDefinitions.ObsoleteKeys.Contains(c.Key)).ToListAsync(cancellationToken);
        _db.Configs.RemoveRange(obsolete);

        var existingKeys = (await _db.Configs.Select(c => c.Key).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var definition in SettingDefinitions.All.Where(definition => !existingKeys.Contains(definition.Key)))
        {
            _db.Configs.Add(new ConfigEntry { Key = definition.Key, Value = definition.Default });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM config", cancellationToken);
        await InsertDefaultsAsync(cancellationToken);
    }

    private async Task<SettingsSaveResult> WriteAsync(
        IReadOnlyDictionary<string, string> changes,
        Func<SettingDefinition, bool> changeableHere,
        string changedElsewhere,
        CancellationToken cancellationToken)
    {
        if (changes.Count == 0) return new SettingsSaveResult("You need to provide at least one setting");

        foreach (var (key, value) in changes)
        {
            if (SettingDefinitions.Find(key) is not { } definition) return new SettingsSaveResult($"There's no setting called {key}");
            if (!changeableHere(definition)) return new SettingsSaveResult(changedElsewhere);
            if (definition.ProblemWith(value) is { } problem) return new SettingsSaveResult(problem);
        }

        var keys = changes.Keys.ToList();
        var stored = await _db.Configs.Where(entry => keys.Contains(entry.Key)).ToDictionaryAsync(entry => entry.Key, cancellationToken);
        foreach (var (key, value) in changes)
        {
            if (stored.TryGetValue(key, out var entry)) entry.Value = value;
            else _db.Configs.Add(new ConfigEntry { Key = key, Value = value });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SettingsSaveResult.Saved;
    }
}
