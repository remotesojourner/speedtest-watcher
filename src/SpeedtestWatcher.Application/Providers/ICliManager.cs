namespace SpeedtestWatcher.Application.Providers;

public interface ICliManager
{
    Task EnsureBinariesAsync(CancellationToken cancellationToken = default);
    string GetBinaryPath(SpeedtestProvider provider);
    bool IsBinaryAvailable(SpeedtestProvider provider);
}
