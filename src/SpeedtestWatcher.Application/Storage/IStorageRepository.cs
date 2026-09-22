namespace SpeedtestWatcher.Application.Storage;

public interface IStorageRepository
{
    Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default);
}
