using SpeedtestWatcher.Web.Resources;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services;

namespace SpeedtestWatcher.Web.Services;

public sealed class SettingsStateService
{
    public static string SaveFailed => WebStrings.SettingsSaveFailed;

    private readonly SettingsService _settings;

    public SettingsStateService(SettingsService settings)
    {
        _settings = settings;
    }

    public AppSettings Current { get; private set; } = AppSettings.Defaults;

    public event Action? OnChange;

    public async Task LoadAsync()
    {
        Current = await _settings.GetAsync();
        OnChange?.Invoke();
    }

    public async Task<OperationResult> SaveAsync(params (string Key, string Value)[] changes)
    {
        if (changes.Length == 0) return OperationResult.Ok();

        var result = await _settings.SaveAsync(changes.ToDictionary(change => change.Key, change => change.Value));
        if (result.Succeeded) await LoadAsync();
        return result;
    }
}
