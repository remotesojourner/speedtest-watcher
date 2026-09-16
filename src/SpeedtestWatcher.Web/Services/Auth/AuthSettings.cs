using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Web.Services.Auth;

/// <summary>The sign-in settings as saved, and what they add up to.</summary>
public sealed record AuthSnapshot(
    bool Enabled,
    string VisitorAccess,
    string? Authority,
    string? ClientId,
    string? ClientSecret,
    IReadOnlyList<string> Scopes,
    string? ApiTokenHash,
    bool DisabledByEnvironment,
    bool PreviewMode)
{
    public static readonly AuthSnapshot Default = new(false, "none", null, null, null, ["openid", "profile", "email"], null, false, false);

    public bool Configured => !string.IsNullOrEmpty(Authority) && !string.IsNullOrEmpty(ClientId);

    /// <summary>Sign-in is enforced: switched on and configured, and neither DISABLE_AUTH nor demo mode overrides it.</summary>
    public bool IsActive => Enabled && Configured && !DisabledByEnvironment && !PreviewMode;
}

/// <summary>
/// The sign-in settings every request is checked against. They're read from the database at startup and
/// again whenever they change, rather than on each request, and a change takes effect without a restart.
/// </summary>
public sealed class AuthSettings
{
    public const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    /// <summary>Config keys owned by the Security tab, which saves them together; the generic config endpoint refuses them.</summary>
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

    /// <summary>DISABLE_AUTH turns sign-in off whatever is saved, so a broken provider can't lock anyone out.</summary>
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
                DisabledByEnvironment: DisabledByEnvironment,
                PreviewMode: Environment.GetEnvironmentVariable("PREVIEW_MODE") == "true");

            Current = snapshot;

            // Handler options are built once and cached; dropping them makes the next sign-in use the new values.
            _oidcOptions.TryRemove(OidcScheme);

            // The OpenID Connect handler refuses to start without a provider and client ID, and the authentication
            // middleware starts it on every request while its scheme exists. So the scheme only exists while in use.
            bool registered = await _schemes.GetSchemeAsync(OidcScheme) != null;
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

        // openid is what makes this OpenID Connect rather than plain OAuth, so it's always requested.
        if (!scopes.Contains("openid")) scopes.Insert(0, "openid");
        return scopes;
    }
}
