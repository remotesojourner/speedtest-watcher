using System.Text.Json;

namespace SpeedtestWatcher.Application.SignIn;

internal class OidcDiscoveryChecker : IOidcDiscovery
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OidcDiscoveryChecker(IHttpClientFactory httpClientFactory)
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
                return $"The provider answered {(int)response.StatusCode} for {url}. Check the provider URL.";

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("authorization_endpoint", out _)
                || !root.TryGetProperty("token_endpoint", out _))
            {
                return $"{url} isn't an OpenID Connect discovery document. Check the provider URL.";
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return $"Couldn't read {url}: {ex.Message}";
        }
    }
}
