using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed partial class ServerSelectionService
{
    private readonly IServerListService _serverLists;
    private readonly ILogger<ServerSelectionService> _logger;

    public ServerSelectionService(IServerListService serverLists, ILogger<ServerSelectionService> logger)
    {
        _serverLists = serverLists;
        _logger = logger;
    }

    public async Task<string?> SelectAsync(ProviderSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings.ServersFor(settings.Selected) is not { } servers) return null;

        return settings.ServerMode switch
        {
            ServerMode.Pinned => servers.PinnedId,
            ServerMode.Random => await RandomServerAsync(settings, servers, cancellationToken),
            _ => null
        };
    }

    private async Task<string?> RandomServerAsync(ProviderSettings settings, ServerChoice servers, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> candidates = settings.ServerListMode == ServerListMode.Deny
            ? (await _serverLists.GetServersAsync(settings.Selected, cancellationToken) ?? [])
                .Select(server => server.Id)
                .Where(id => !servers.ListedIds.Contains(id))
                .ToList()
            : servers.ListedIds;

        if (candidates.Count == 0)
        {
            LogNoServers(settings.Selected);
            return null;
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No {Provider} servers to choose from; letting the provider decide")]
    private partial void LogNoServers(SpeedtestProvider provider);
}
