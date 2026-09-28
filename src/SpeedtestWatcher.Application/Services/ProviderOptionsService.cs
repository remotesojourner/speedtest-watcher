using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Extensions;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed class ProviderOptionsService
{
    private readonly IServerListService _serverLists;
    private readonly INetworkInterfaceService _interfaces;

    public ProviderOptionsService(IServerListService serverLists, INetworkInterfaceService interfaces)
    {
        _serverLists = serverLists;
        _interfaces = interfaces;
    }

    public async Task<OperationResult<IReadOnlyList<ServerInfo>>> GetServersAsync(string provider, CancellationToken cancellationToken = default) =>
        EnumExtensions.TryParse<SpeedtestProvider>(provider, out var chosen) && await _serverLists.GetServersAsync(chosen, cancellationToken) is { } servers
            ? OperationResult.Ok(servers)
            : OperationResult.Invalid(ApplicationStrings.ProviderInvalid);

    public Task<Dictionary<string, List<string>>> GetInterfacesAsync(CancellationToken cancellationToken = default) =>
        _interfaces.GetInterfacesAsync(cancellationToken: cancellationToken);
}
