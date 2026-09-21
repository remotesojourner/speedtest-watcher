using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Settings;
using SpeedtestWatcher.Infrastructure.Network;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

public class ServerSelector
{
    private readonly ServerListProvider _serverList;
    private readonly ILogger<ServerSelector> _logger;

    public ServerSelector(ServerListProvider serverList, ILogger<ServerSelector> logger)
    {
        _serverList = serverList;
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
            ? (await NearbyServerIdsAsync(settings.Selected, cancellationToken)).Where(id => !servers.ListedIds.Contains(id)).ToList()
            : servers.ListedIds;

        if (candidates.Count == 0)
        {
            _logger.LogInformation("No {Provider} servers to choose from; letting the provider decide", settings.Selected);
            return null;
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private async Task<List<string>> NearbyServerIdsAsync(SpeedtestProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            if (await _serverList.GetServersAsync(provider, cancellationToken) is JsonElement { ValueKind: JsonValueKind.Object } element)
                return element.EnumerateObject().Select(property => property.Name).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Could not read the {Provider} server list", provider);
        }

        return [];
    }
}
