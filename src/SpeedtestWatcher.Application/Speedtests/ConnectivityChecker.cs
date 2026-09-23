using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Application.Speedtests;

internal partial class ConnectivityChecker : IConnectivityChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConnectionState _connection;
    private readonly ILogger<ConnectivityChecker> _logger;

    public ConnectivityChecker(IHttpClientFactory httpClientFactory, ConnectionState connection, ILogger<ConnectivityChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _connection = connection;
        _logger = logger;
    }

    public async Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default)
    {
        if (_connection.Current.IsDown) return new PreTestCheck(false, "No internet connection: the monitor has the line down");
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
            LogCheckFailed(ex, url);
            return new PreTestCheck(false, "No internet connection");
        }

        if (settings.SkipIps.Count > 0 && publicIp.Length > 0 && settings.SkipIps.Contains(publicIp, StringComparer.OrdinalIgnoreCase))
            return new PreTestCheck(false, $"Public IP {publicIp} is on the skip list", publicIp);

        return new PreTestCheck(true, null, publicIp.Length > 0 ? publicIp : null);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connectivity check against {Url} failed")]
    private partial void LogCheckFailed(Exception exception, string url);
}
