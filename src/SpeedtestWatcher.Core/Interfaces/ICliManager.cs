using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.Interfaces;

public interface ICliManager
{
    Task EnsureBinariesAsync(CancellationToken cancellationToken = default);
    string GetBinaryPath(SpeedtestProvider provider);
    bool IsBinaryAvailable(SpeedtestProvider provider);
}
