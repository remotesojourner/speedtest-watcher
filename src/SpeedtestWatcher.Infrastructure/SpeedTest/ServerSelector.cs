using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Network;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

/// <summary>
/// Picks the server for a run: automatic (the provider decides), random from the configured list,
/// or a single fixed server.
/// </summary>
public class ServerSelector
{
    private readonly IConfigRepository _configRepo;
    private readonly ServerListProvider _serverList;
    private readonly ILogger<ServerSelector> _logger;

    public ServerSelector(IConfigRepository configRepo, ServerListProvider serverList, ILogger<ServerSelector> logger)
    {
        _configRepo = configRepo;
        _serverList = serverList;
        _logger = logger;
    }

    /// <summary>The server id to test against, or null to let the provider choose.</summary>
    public async Task<string?> SelectAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default)
    {
        // Cloudflare always picks its own edge location.
        if (provider != SpeedtestProvider.Ookla && provider != SpeedtestProvider.Libre) return null;

        string providerKey = provider == SpeedtestProvider.Ookla ? "ookla" : "libre";
        string mode = Value(await _configRepo.GetValueAsync("serverMode", cancellationToken)) ?? "auto";

        if (mode == "single")
            return Value(await _configRepo.GetValueAsync($"{providerKey}Id", cancellationToken));

        if (mode != "random") return null;

        var listed = ParseIds(await _configRepo.GetValueAsync($"{providerKey}ServerIds", cancellationToken));
        bool deny = Value(await _configRepo.GetValueAsync("serverListMode", cancellationToken)) == "deny";

        var candidates = deny
            ? (await NearbyServerIdsAsync(providerKey, cancellationToken)).Where(id => !listed.Contains(id)).ToList()
            : listed;

        if (candidates.Count == 0)
        {
            _logger.LogInformation("No {Provider} servers to choose from; letting the provider decide", providerKey);
            return null;
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private async Task<List<string>> NearbyServerIdsAsync(string providerKey, CancellationToken cancellationToken)
    {
        try
        {
            // The cached list is an object keyed by server id.
            if (await _serverList.GetServersAsync(providerKey, cancellationToken) is JsonElement { ValueKind: JsonValueKind.Object } element)
                return element.EnumerateObject().Select(property => property.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the {Provider} server list", providerKey);
        }

        return [];
    }

    private static List<string> ParseIds(string? raw) =>
        Value(raw) is { } value
            ? value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : [];

    private static string? Value(string? raw) =>
        string.IsNullOrWhiteSpace(raw) || raw == "none" ? null : raw.Trim();
}
