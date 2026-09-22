using System.Text.Json;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Settings;

public sealed class SettingsBackupService
{
    private readonly ISettingsStore _store;
    private readonly SettingsService _settings;
    private readonly IIntegrationRepository _integrations;
    private readonly IRecommendationRepository _recommendations;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly ICurrentAccess _access;

    public SettingsBackupService(
        ISettingsStore store,
        SettingsService settings,
        IIntegrationRepository integrations,
        IRecommendationRepository recommendations,
        IIntegrationDispatcher dispatcher,
        ICurrentAccess access)
    {
        _store = store;
        _settings = settings;
        _integrations = integrations;
        _recommendations = recommendations;
        _dispatcher = dispatcher;
        _access = access;
    }

    public async Task<OperationResult<SettingsBackupDto>> ExportAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return OperationResult.Ok(new SettingsBackupDto
        {
            Config = (await _store.GetValuesAsync(cancellationToken))
                .Where(setting => !SettingDefinitions.Find(setting.Key)!.IsManagedOnSecurityTab)
                .OrderBy(setting => setting.Key, StringComparer.Ordinal)
                .Select(setting => new ConfigEntry { Key = setting.Key, Value = setting.Value })
                .ToList(),
            Integrations = await _integrations.ListAllAsync(cancellationToken),
            Recommendations = await _recommendations.GetAsync(cancellationToken)
        });
    }

    public async Task<OperationResult<SettingsImportResultDto>> ImportAsync(SettingsBackupDto backup, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

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

        foreach (var integration in backup.Integrations)
        {
            if (_dispatcher.Schemas.TryGetValue(integration.Name, out var schema) && IsJsonObject(integration.Data))
            {
                if (schema.Fields.Any(field => field.Name == HealthyAgainToggle.Key))
                    integration.Data = HealthyAgainToggle.FollowUnhealthy(integration.Data);

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

        return OperationResult.Ok(result);
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
