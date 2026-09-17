using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Hosting;

namespace SpeedtestWatcher.Infrastructure.Network;

public class ServerListProvider
{
    private static readonly JsonSerializerOptions CacheFileJson = new() { WriteIndented = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServerListProvider> _logger;
    private readonly string _serversDir;

    public ServerListProvider(IHttpClientFactory httpClientFactory, IOptions<SpeedtestWatcherOptions> options, ILogger<ServerListProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serversDir = options.Value.ServersDirectory;
    }

    public async Task<object?> GetServersAsync(string provider, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_serversDir);
        var filePath = Path.Combine(_serversDir, $"{provider}.json");

        if (File.Exists(filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize<object>(json);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex, "The cached {Provider} server list can't be read, so it is being downloaded again", provider);
            }
        }

        await RefreshServersAsync(provider, cancellationToken);

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<object>(json);
        }

        return new Dictionary<string, object>();
    }

    public async Task RefreshServersAsync(string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_serversDir);
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var url = provider == "ookla"
                ? "https://www.speedtest.net/api/js/servers?limit=20"
                : "https://librespeed.org/backend-servers/servers.php";

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var dict = new Dictionary<string, object>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in doc.RootElement.EnumerateArray())
                {
                    if (row.TryGetProperty("id", out var idElem))
                    {
                        var id = idElem.ToString();
                        if (provider == "ookla")
                        {
                            dict[id] = new
                            {
                                name = row.TryGetProperty("name", out var n) ? n.GetString() : null,
                                sponsor = row.TryGetProperty("sponsor", out var sp) ? sp.GetString() : null,
                                country = row.TryGetProperty("country", out var c) ? c.GetString() : null,
                                cc = row.TryGetProperty("cc", out var cc) ? cc.GetString() : null,
                                distance = row.TryGetProperty("distance", out var d) ? d.GetDouble() : 0,
                                host = row.TryGetProperty("host", out var h) ? h.GetString() : null
                            };
                        }
                        else
                        {
                            dict[id] = (row.TryGetProperty("name", out var n) ? n.GetString() : null) ?? id;
                        }
                    }
                }
            }

            var filePath = Path.Combine(_serversDir, $"{provider}.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(dict, CacheFileJson), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or IOException or UnauthorizedAccessException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Could not load {Provider} server list", provider);
        }
    }
}
