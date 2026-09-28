using SpeedtestWatcher.Application.Resources;
using System.Text.Json;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

internal class OidcDiscoveryService : IOidcDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OidcDiscoveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> FindProblemAsync(string authority, CancellationToken cancellationToken = default)
    {
        var url = authority.TrimEnd('/') + "/.well-known/openid-configuration";
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ApplicationStrings.Format(ApplicationStrings.OidcProviderAnswered, (int)response.StatusCode, url);

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("authorization_endpoint", out _)
                || !root.TryGetProperty("token_endpoint", out _))
            {
                return ApplicationStrings.Format(ApplicationStrings.OidcNotDiscoveryDocument, url);
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return ApplicationStrings.Format(ApplicationStrings.OidcUnreadable, url, ex.Message);
        }
    }
}
