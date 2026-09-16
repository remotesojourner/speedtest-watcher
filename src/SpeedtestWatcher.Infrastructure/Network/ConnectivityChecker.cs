using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.Network;

public record PreTestCheck(bool Proceed, string? SkipReason)
{
    public static readonly PreTestCheck Ok = new(true, null);
}

public class ConnectivityChecker
{
    private readonly IConfigRepository _configRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConnectivityChecker> _logger;

    public ConnectivityChecker(IConfigRepository configRepo, IHttpClientFactory httpClientFactory, ILogger<ConnectivityChecker> logger)
    {
        _configRepo = configRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PreTestCheck> CheckAsync(CancellationToken cancellationToken = default)
    {
        var checkEnabled = (await _configRepo.GetValueAsync("internetCheckEnabled", cancellationToken) ?? "true") == "true";
        var skipIps = ParseList(await _configRepo.GetValueAsync("skipIps", cancellationToken));

        if (!checkEnabled && skipIps.Count == 0) return PreTestCheck.Ok;

        var url = Value(await _configRepo.GetValueAsync("internetCheckUrl", cancellationToken)) ?? "https://icanhazip.com";
        string publicIp;
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new PreTestCheck(false, $"No internet connection: {url} answered {(int)response.StatusCode}");

            publicIp = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connectivity check against {Url} failed", url);
            return new PreTestCheck(false, "No internet connection");
        }

        if (skipIps.Count > 0 && publicIp.Length > 0 && skipIps.Contains(publicIp, StringComparer.OrdinalIgnoreCase))
            return new PreTestCheck(false, $"Public IP {publicIp} is on the skip list");

        return PreTestCheck.Ok;
    }

    private static List<string> ParseList(string? raw) =>
        Value(raw) is { } value
            ? value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : [];

    private static string? Value(string? raw) =>
        string.IsNullOrWhiteSpace(raw) || raw == "none" ? null : raw.Trim();
}
