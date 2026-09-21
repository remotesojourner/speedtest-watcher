using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Web.Services;

public sealed class SettingsBackup
{
    private readonly ISettingsStore _store;
    private readonly SettingsService _settings;
    private readonly IIntegrationRepository _integrations;
    private readonly IRecommendationRepository _recommendations;
    private readonly IIntegrationDispatcher _dispatcher;

    public SettingsBackup(
        ISettingsStore store,
        SettingsService settings,
        IIntegrationRepository integrations,
        IRecommendationRepository recommendations,
        IIntegrationDispatcher dispatcher)
    {
        _store = store;
        _settings = settings;
        _integrations = integrations;
        _recommendations = recommendations;
        _dispatcher = dispatcher;
    }

    public async Task<SettingsBackupDto> ExportAsync(CancellationToken cancellationToken = default) => new()
    {
        Config = (await _store.GetValuesAsync(cancellationToken))
            .Where(setting => !SettingDefinitions.Find(setting.Key)!.IsManagedOnSecurityTab)
            .OrderBy(setting => setting.Key, StringComparer.Ordinal)
            .Select(setting => new ConfigEntry { Key = setting.Key, Value = setting.Value })
            .ToList(),
        Integrations = await _integrations.ListAllAsync(cancellationToken),
        Recommendations = await _recommendations.GetAsync(cancellationToken)
    };

    public async Task<SettingsImportResultDto> ImportAsync(SettingsBackupDto backup, CancellationToken cancellationToken = default)
    {
        var result = new SettingsImportResultDto();

        var importable = new Dictionary<string, string>();
        foreach (var entry in backup.Config)
        {
            if (IsImportableSetting(entry))
            {
                importable[entry.Key] = entry.Value;
                result.Settings++;
            }
            else
            {
                result.Skipped++;
            }
        }

        if (importable.Count > 0)
            await _settings.SaveAsync(importable, cancellationToken);

        var integrationTypes = _dispatcher.Schemas;
        foreach (var integration in backup.Integrations)
        {
            if (integrationTypes.ContainsKey(integration.Name) && IsJsonObject(integration.Data))
            {
                await _integrations.UpsertAsync(integration, cancellationToken);
                result.Integrations++;
            }
            else
            {
                result.Skipped++;
            }
        }

        if (backup.Recommendations is { Ping: > 0, Download: > 0, Upload: > 0 } recommendation)
        {
            await _recommendations.SaveAsync(recommendation.Ping, recommendation.Download, recommendation.Upload, cancellationToken);
            result.Recommendations = true;
        }
        else if (backup.Recommendations != null)
        {
            result.Skipped++;
        }

        return result;
    }

    private static bool IsImportableSetting(ConfigEntry entry) =>
        SettingDefinitions.Find(entry.Key) is { IsManagedOnSecurityTab: false } definition
        && definition.ProblemWith(entry.Value) == null;

    private static bool IsJsonObject(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
