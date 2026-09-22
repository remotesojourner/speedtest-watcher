namespace SpeedtestWatcher.Application.Providers;

public interface INetworkInterfaceDetector
{
    Task<Dictionary<string, List<string>>> GetInterfacesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}
