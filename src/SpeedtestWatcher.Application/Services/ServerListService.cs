using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Extensions;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Services.Providers.Interfaces;

namespace SpeedtestWatcher.Application.Services;

internal partial class ServerListService : IServerListService
{
    public static readonly TimeSpan MaxCacheAge = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions _cacheFileJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly Dictionary<SpeedtestProvider, ISpeedtestProviderService> _providerServices;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<ServerListService> _logger;
    private readonly string _serversDir;

    public ServerListService(
        IEnumerable<ISpeedtestProviderService> providerServices,
        IHttpClientFactory httpClientFactory,
        TimeProvider time,
        IOptions<SpeedtestWatcherOptions> options,
        ILogger<ServerListService> logger)
    {
        _providerServices = providerServices.ToDictionary(providerService => providerService.Provider);
        _httpClientFactory = httpClientFactory;
        _time = time;
        _logger = logger;
        _serversDir = options.Value.ServersDirectory;
    }

    public async Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default)
    {
        if (!_providerServices.TryGetValue(provider, out var providerService) || providerService.Servers is not { } catalog) return null;

        var cacheFile = Path.Combine(_serversDir, $"{provider.ToName()}.json");
        var cached = await ReadCacheAsync(cacheFile, provider, cancellationToken);
        if (cached != null && _time.GetUtcNow().UtcDateTime - File.GetLastWriteTimeUtc(cacheFile) < MaxCacheAge) return cached;

        var downloaded = await DownloadAsync(provider, catalog, cancellationToken);
        if (downloaded == null) return cached ?? [];

        await WriteCacheAsync(cacheFile, provider, downloaded, cancellationToken);
        return downloaded;
    }

    private async Task<IReadOnlyList<ServerInfo>?> ReadCacheAsync(string cacheFile, SpeedtestProvider provider, CancellationToken cancellationToken)
    {
        if (!File.Exists(cacheFile)) return null;

        try
        {
            await using var stream = File.OpenRead(cacheFile);
            return await JsonSerializer.DeserializeAsync<List<ServerInfo>>(stream, _cacheFileJson, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            LogCacheUnreadable(ex, provider);
            return null;
        }
    }

    private async Task<IReadOnlyList<ServerInfo>?> DownloadAsync(SpeedtestProvider provider, ServerCatalog catalog, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.GetAsync(catalog.ListUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogListRefused(provider, (int)response.StatusCode);
                return null;
            }

            return catalog.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or FormatException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogListNotLoaded(ex, provider);
            return null;
        }
    }

    private async Task WriteCacheAsync(string cacheFile, SpeedtestProvider provider, IReadOnlyList<ServerInfo> servers, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_serversDir);
            await File.WriteAllTextAsync(cacheFile, JsonSerializer.Serialize(servers, _cacheFileJson), cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogListNotCached(ex, provider);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The cached {Provider} server list can't be read, so it is being downloaded again")]
    private partial void LogCacheUnreadable(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The {Provider} server list answered {Status}")]
    private partial void LogListRefused(SpeedtestProvider provider, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load {Provider} server list")]
    private partial void LogListNotLoaded(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not cache the {Provider} server list")]
    private partial void LogListNotCached(Exception exception, SpeedtestProvider provider);
}
