using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace SpeedtestWatcher.Application.Providers;

internal sealed class InterfaceDetector : INetworkInterfaceDetector, IDisposable
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
