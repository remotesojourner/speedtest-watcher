using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Services;

public sealed class SettingsBackup
{
    private readonly IConfigRepository _config;
    private readonly IIntegrationRepository _integrations;
    private readonly IRecommendationRepository _recommendations;
    private readonly IIntegrationDispatcher _dispatcher;

    public SettingsBackup(
        IConfigRepository config,
        IIntegrationRepository integrations,
        IRecommendationRepository recommendations,
        IIntegrationDispatcher dispatcher)
    {
        _config = config;
        _integrations = integrations;
        _recommendations = recommendations;
        _dispatcher = dispatcher;
    }

    public async Task<SettingsBackupDto> ExportAsync(CancellationToken cancellationToken = default) => new()
    {
        Config = (await _config.ListAllAsync(cancellationToken))
            .Where(entry => !AuthSettings.Keys.Contains(entry.Key))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToList(),
        Integrations = await _integrations.ListAllAsync(cancellationToken),
        Recommendations = await _recommendations.GetAsync(cancellationToken)
    };

    public async Task<SettingsImportResultDto> ImportAsync(SettingsBackupDto backup, CancellationToken cancellationToken = default)
    {
        var result = new SettingsImportResultDto();

        foreach (var entry in backup.Config)
        {
            if (await IsImportableSettingAsync(entry, cancellationToken))
            {
                await _config.UpdateValueAsync(entry.Key, entry.Value, cancellationToken);
                result.Settings++;
            }
            else
            {
                result.Skipped++;
            }
        }

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

    private async Task<bool> IsImportableSettingAsync(ConfigEntry entry, CancellationToken cancellationToken) =>
        !AuthSettings.Keys.Contains(entry.Key)
        && ConfigRepository.ConfigDefaults.ContainsKey(entry.Key)
        && await _config.ValidateInputAsync(entry.Key, entry.Value, cancellationToken) == null;

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
