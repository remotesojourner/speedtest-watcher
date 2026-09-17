namespace SpeedtestWatcher.Core.Interfaces;

public interface IStorageRepository
{
    Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default);
}
