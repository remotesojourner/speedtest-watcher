using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Resources;

namespace SpeedtestWatcher.Application.Providers;

public sealed class ProviderOptionsService
{
    private readonly IServerListProvider _serverLists;
    private readonly INetworkInterfaceDetector _interfaces;

    public ProviderOptionsService(IServerListProvider serverLists, INetworkInterfaceDetector interfaces)
    {
        _serverLists = serverLists;
        _interfaces = interfaces;
    }

    public async Task<OperationResult<IReadOnlyList<ServerInfo>>> GetServersAsync(string provider, CancellationToken cancellationToken = default) =>
        EnumNames.TryParse<SpeedtestProvider>(provider, out var chosen) && await _serverLists.GetServersAsync(chosen, cancellationToken) is { } servers
            ? OperationResult.Ok(servers)
            : OperationResult.Invalid(ApplicationStrings.ProviderInvalid);

    public Task<Dictionary<string, List<string>>> GetInterfacesAsync(CancellationToken cancellationToken = default) =>
        _interfaces.GetInterfacesAsync(cancellationToken: cancellationToken);
}
