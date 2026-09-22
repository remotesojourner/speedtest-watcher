using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.SpeedTest;

namespace SpeedtestWatcher.Application.Info;

public sealed class SystemInfoService
{
    private readonly IReleaseChecker _releases;
    private readonly IServerListProvider _serverLists;
    private readonly INetworkInterfaceDetector _interfaces;

    public SystemInfoService(IReleaseChecker releases, IServerListProvider serverLists, INetworkInterfaceDetector interfaces)
    {
        _releases = releases;
        _serverLists = serverLists;
        _interfaces = interfaces;
    }

    public async Task<VersionInfoDto> GetVersionAsync(CancellationToken cancellationToken = default) => new()
    {
        Local = ProjectInfo.Version,
        Remote = await _releases.GetLatestVersionAsync(cancellationToken) ?? "0"
    };

    public async Task<OperationResult<IReadOnlyList<ServerInfo>>> GetServersAsync(string provider, CancellationToken cancellationToken = default) =>
        EnumNames.TryParse<SpeedtestProvider>(provider, out var chosen) && await _serverLists.GetServersAsync(chosen, cancellationToken) is { } servers
            ? OperationResult.Ok(servers)
            : OperationResult.Invalid("Invalid provider");

    public Task<Dictionary<string, List<string>>> GetInterfacesAsync(CancellationToken cancellationToken = default) =>
        _interfaces.GetInterfacesAsync(cancellationToken: cancellationToken);
}
