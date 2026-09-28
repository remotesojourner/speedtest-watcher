namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface INetworkInterfaceService
{
    Task<Dictionary<string, List<string>>> GetInterfacesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}
