using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SpeedtestWatcher.Web.SignIn;

public sealed partial class OidcOptionsSetup : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly AuthSettings _settings;
    private readonly ILogger<OidcOptionsSetup> _logger;

    public OidcOptionsSetup(AuthSettings settings, ILogger<OidcOptionsSetup> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (name != AuthSettings.OidcScheme) return;

        var current = _settings.Current;
        options.Authority = current.Authority;
        options.ClientId = current.ClientId;
        options.ClientSecret = current.ClientSecret;
        options.RequireHttpsMetadata = current.Authority?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true;

        options.Scope.Clear();
        foreach (var scope in current.Scopes) options.Scope.Add(scope);

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;

        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = false;
        options.TokenValidationParameters.NameClaimType = "name";

        options.Events.OnRemoteFailure = context =>
        {
            LogSignInFailed(context.Failure);
            context.Response.Redirect($"/auth/failed?reason={Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error")}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    }

    public void Configure(OpenIdConnectOptions options) => Configure(Options.DefaultName, options);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-in with the OpenID Connect provider failed")]
    private partial void LogSignInFailed(Exception? exception);
}
