using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Core.Interfaces;

public interface ISettingsStore
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(CancellationToken cancellationToken = default);
    Task<SettingsSaveResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default);
    Task<SettingsSaveResult> SaveSignInAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default);
    Task InsertDefaultsAsync(CancellationToken cancellationToken = default);
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
