using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Interfaces;

public interface IConfigRepository
{
    Task<List<ConfigEntry>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task UpdateValueAsync(string key, string value, CancellationToken cancellationToken = default);
    Task InsertDefaultsAsync(CancellationToken cancellationToken = default);
    Task<string?> ValidateInputAsync(string key, object? value, CancellationToken cancellationToken = default);
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
