using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.SignIn;

public sealed record AuthSnapshot(
    bool Enabled,
    VisitorAccess VisitorAccess,
    string? Authority,
    string? ClientId,
    string? ClientSecret,
    IReadOnlyList<string> Scopes,
    string? ApiTokenHash,
    bool DisabledByEnvironment)
{
    public static readonly AuthSnapshot Default = new(false, VisitorAccess.None, null, null, null, ["openid", "profile", "email"], null, false);

    public bool Configured => !string.IsNullOrEmpty(Authority) && !string.IsNullOrEmpty(ClientId);

    public bool IsActive => Enabled && Configured && !DisabledByEnvironment;
}

public sealed class AuthSettings : ISignInState, IDisposable
{
    public const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IOptionsMonitorCache<OpenIdConnectOptions> _oidcOptions;
    private readonly bool _disabledByEnvironment;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public AuthSettings(
        IServiceScopeFactory scopeFactory,
        IAuthenticationSchemeProvider schemes,
        IOptionsMonitorCache<OpenIdConnectOptions> oidcOptions,
        IOptions<SpeedtestWatcherOptions> options)
    {
        _scopeFactory = scopeFactory;
        _schemes = schemes;
        _oidcOptions = oidcOptions;
        _disabledByEnvironment = options.Value.DisableAuth;
    }

    public AuthSnapshot Current { get; private set; } = AuthSnapshot.Default;

    public bool IsActive => Current.IsActive;

    public bool DisabledByEnvironment => Current.DisabledByEnvironment;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var signIn = (await scope.ServiceProvider.GetRequiredService<ISettingsStore>().GetAsync(cancellationToken)).SignIn;

            var snapshot = new AuthSnapshot(
                Enabled: signIn.Enabled,
                VisitorAccess: signIn.VisitorAccess,
                Authority: signIn.Authority,
                ClientId: signIn.ClientId,
                ClientSecret: signIn.ClientSecret,
                Scopes: signIn.Scopes,
                ApiTokenHash: signIn.ApiTokenHash,
                DisabledByEnvironment: _disabledByEnvironment);

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

    public void Dispose() => _reloadLock.Dispose();
}
