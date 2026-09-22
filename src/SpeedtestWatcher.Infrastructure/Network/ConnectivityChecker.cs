using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Infrastructure.Network;

public class ConnectivityChecker : IConnectivityChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConnectivityChecker> _logger;

    public ConnectivityChecker(IHttpClientFactory httpClientFactory, ILogger<ConnectivityChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default)
    {
        if (!settings.InternetCheckEnabled && settings.SkipIps.Count == 0) return PreTestCheck.Ok;

        var url = settings.InternetCheckUrl;
        string publicIp;
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new PreTestCheck(false, $"No internet connection: {url} answered {(int)response.StatusCode}");

            publicIp = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or UriFormatException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Connectivity check against {Url} failed", url);
            return new PreTestCheck(false, "No internet connection");
        }

        if (settings.SkipIps.Count > 0 && publicIp.Length > 0 && settings.SkipIps.Contains(publicIp, StringComparer.OrdinalIgnoreCase))
            return new PreTestCheck(false, $"Public IP {publicIp} is on the skip list");

        return PreTestCheck.Ok;
    }
}
