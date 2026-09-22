using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Web.Ui.State;

public sealed class SettingsState
{
    public const string SaveFailed = "Could not save your changes. Try again.";

    private readonly SettingsService _settings;

    public SettingsState(SettingsService settings)
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
