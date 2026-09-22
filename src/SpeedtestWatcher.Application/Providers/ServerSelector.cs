using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Application.Providers;

public sealed class ServerSelector
{
    private readonly IServerListProvider _serverLists;
    private readonly ILogger<ServerSelector> _logger;

    public ServerSelector(IServerListProvider serverLists, ILogger<ServerSelector> logger)
    {
        _serverLists = serverLists;
        _logger = logger;
    }

    public async Task<string?> SelectAsync(ProviderSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings.ServersFor(settings.Selected) is not { } servers) return null;

        return settings.ServerMode switch
        {
            ServerMode.Single => servers.SingleId,
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
            _logger.LogInformation("No {Provider} servers to choose from; letting the provider decide", settings.Selected);
            return null;
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }
}
