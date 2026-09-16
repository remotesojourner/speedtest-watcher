using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed record AuthSnapshot(
    bool Enabled,
    string VisitorAccess,
    string? Authority,
    string? ClientId,
    string? ClientSecret,
    IReadOnlyList<string> Scopes,
    string? ApiTokenHash,
    bool DisabledByEnvironment)
{
    public static readonly AuthSnapshot Default = new(false, "none", null, null, null, ["openid", "profile", "email"], null, false);

    public bool Configured => !string.IsNullOrEmpty(Authority) && !string.IsNullOrEmpty(ClientId);

    public bool IsActive => Enabled && Configured && !DisabledByEnvironment;
}

public sealed class AuthSettings
{
    public const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    public static readonly IReadOnlySet<string> Keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "authEnabled", "visitorAccess", "oidcAuthority", "oidcClientId", "oidcClientSecret", "oidcScopes", "apiTokenHash"
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IOptionsMonitorCache<OpenIdConnectOptions> _oidcOptions;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public AuthSettings(
        IServiceScopeFactory scopeFactory,
        IAuthenticationSchemeProvider schemes,
        IOptionsMonitorCache<OpenIdConnectOptions> oidcOptions)
    {
        _scopeFactory = scopeFactory;
        _schemes = schemes;
        _oidcOptions = oidcOptions;
    }

    public AuthSnapshot Current { get; private set; } = AuthSnapshot.Default;

    public static bool DisabledByEnvironment =>
        Environment.GetEnvironmentVariable("DISABLE_AUTH")?.Trim().ToLowerInvariant() is "true" or "1" or "yes";

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfigRepository>();

            async Task<string?> ReadAsync(string key)
            {
                var value = await config.GetValueAsync(key, cancellationToken);
                return string.IsNullOrWhiteSpace(value) || value == "none" ? null : value;
            }

            var snapshot = new AuthSnapshot(
                Enabled: await ReadAsync("authEnabled") == "true",
                VisitorAccess: await ReadAsync("visitorAccess") == "read" ? "read" : "none",
                Authority: await ReadAsync("oidcAuthority"),
                ClientId: await ReadAsync("oidcClientId"),
                ClientSecret: await ReadAsync("oidcClientSecret"),
                Scopes: ParseScopes(await ReadAsync("oidcScopes")),
                ApiTokenHash: await ReadAsync("apiTokenHash"),
                DisabledByEnvironment: DisabledByEnvironment);

            Current = snapshot;

            _oidcOptions.TryRemove(OidcScheme);

            var registered = await _schemes.GetSchemeAsync(OidcScheme) != null;
            if (snapshot.IsActive && !registered)
                _schemes.TryAddScheme(new AuthenticationScheme(OidcScheme, "OpenID Connect", typeof(OpenIdConnectHandler)));
            else if (!snapshot.IsActive && registered)
                _schemes.RemoveScheme(OidcScheme);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public static IReadOnlyList<string> ParseScopes(string? raw)
    {
        var scopes = (raw ?? "")
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!scopes.Contains("openid")) scopes.Insert(0, "openid");
        return scopes;
    }
}
