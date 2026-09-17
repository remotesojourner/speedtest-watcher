using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.Network;

public sealed class InterfaceDetector : INetworkInterfaceDetector, IDisposable
{
    private readonly ILogger<InterfaceDetector> _logger;
    private Dictionary<string, List<string>> _cachedInterfaces = new();
    private DateTime _lastScan = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public InterfaceDetector(ILogger<InterfaceDetector> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, List<string>>> GetInterfacesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _cachedInterfaces.Count > 0 && DateTime.UtcNow - _lastScan < TimeSpan.FromHours(1))
        {
            return _cachedInterfaces;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _cachedInterfaces.Count > 0 && DateTime.UtcNow - _lastScan < TimeSpan.FromHours(1))
            {
                return _cachedInterfaces;
            }

            var result = new Dictionary<string, List<string>>();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var ipProps = ni.GetIPProperties();
                var unicastAddresses = ipProps.UnicastAddresses;

                var ips = new List<string>();
                foreach (var addr in unicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork ||
                        addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (!IPAddress.IsLoopback(addr.Address))
                        {
                            ips.Add(addr.Address.ToString());
                        }
                    }
                }

                if (ips.Count > 0)
                {
                    result[ni.Name] = ips;
                }
            }

            _cachedInterfaces = result;
            _lastScan = DateTime.UtcNow;
            _logger.LogInformation("Detected {Count} active network interfaces", result.Count);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();
}

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load {Provider} server list", provider);
        }
    }
}
