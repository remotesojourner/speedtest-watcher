namespace SpeedtestWatcher.Core.Interfaces;

public interface INetworkInterfaceDetector
{
    Task<Dictionary<string, List<string>>> GetInterfacesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}
