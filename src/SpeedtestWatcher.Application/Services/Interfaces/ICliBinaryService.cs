using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface ICliBinaryService
{
    Task EnsureBinariesAsync(CancellationToken cancellationToken = default);
    string GetBinaryPath(SpeedtestProvider provider);
    bool IsBinaryAvailable(SpeedtestProvider provider);
}
