using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthSettings _auth;
    private readonly IConfigRepository _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(AuthSettings auth, IConfigRepository config, IHttpClientFactory httpClientFactory)
    {
        _auth = auth;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    private bool IsViewMode => HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;

    [HttpGet("/auth/login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        if (!_auth.Current.IsActive || User.Identity?.IsAuthenticated == true) return LocalRedirect(target);

        return Challenge(new AuthenticationProperties { RedirectUri = target }, AuthSettings.OidcScheme);
    }

    [HttpGet("/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return _auth.Current.IsActive ? LocalRedirect("/auth/signed-out") : LocalRedirect("/");
    }

    [HttpGet("/auth/signed-out")]
    public ContentResult SignedOut() => MessagePage(
        "You've signed out",
        "You're signed out of Speedtest Watcher. Your identity provider may still have you signed in there.",
        null,
        "/auth/login",
        "Sign in again");

    [HttpGet("/auth/failed")]
    public ContentResult Failed([FromQuery] string? reason) => MessagePage(
        "Sign-in didn't work",
        string.IsNullOrWhiteSpace(reason) ? "The identity provider didn't complete the sign-in." : reason,
        "If the sign-in settings are wrong, start Speedtest Watcher with DISABLE_AUTH=true. Sign-in is then off, so you can correct the settings on the Security tab and remove the variable again.",
        "/auth/login",
        "Try again");

    [HttpPut("/api/auth/settings")]
    public async Task<IActionResult> SaveSettings([FromBody] AuthSettingsRequest request, CancellationToken cancellationToken)
    {
        if (IsViewMode) return Unauthorized(new { message = "Authentication required" });

        if (request.VisitorAccess is not ("none" or "read"))
            return BadRequest(new { message = "Visitor access must be none or read" });

        var authority = Clean(request.Authority);
        if (authority != null
            && !(Uri.TryCreate(authority, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
        {
            return BadRequest(new { message = "The provider URL must be a full http(s) URL" });
        }

        var clientId = Clean(request.ClientId);
        if (request.Enabled)
        {
            if (authority == null || clientId == null)
                return BadRequest(new { message = "Sign-in needs the provider URL and a client ID" });

            var problem = await CheckDiscoveryAsync(authority, cancellationToken);
            if (problem != null) return BadRequest(new { message = problem });
        }

        var values = new Dictionary<string, string>
        {
            ["authEnabled"] = request.Enabled ? "true" : "false",
            ["visitorAccess"] = request.VisitorAccess,
            ["oidcAuthority"] = authority ?? "none",
            ["oidcClientId"] = clientId ?? "none",
            ["oidcScopes"] = string.Join(' ', AuthSettings.ParseScopes(request.Scopes))
        };
        if (Clean(request.ClientSecret) is { } secret) values["oidcClientSecret"] = secret;
        else if (request.ClearClientSecret) values["oidcClientSecret"] = "none";

        foreach (var (key, value) in values)
            await _config.UpdateValueAsync(key, value, cancellationToken);

        await _auth.ReloadAsync(cancellationToken);
        return Ok(new { message = "Sign-in settings saved", active = _auth.Current.IsActive });
    }

    [HttpPost("/api/auth/token")]
    public async Task<IActionResult> CreateToken(CancellationToken cancellationToken)
    {
        if (IsViewMode) return Unauthorized(new { message = "Authentication required" });

        var token = ApiToken.Generate();
        await _config.UpdateValueAsync("apiTokenHash", ApiToken.Hash(token), cancellationToken);
        await _auth.ReloadAsync(cancellationToken);

        return Ok(new ApiTokenResponse { Token = token });
    }

    [HttpDelete("/api/auth/token")]
    public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken)
    {
        if (IsViewMode) return Unauthorized(new { message = "Authentication required" });

        await _config.UpdateValueAsync("apiTokenHash", "none", cancellationToken);
        await _auth.ReloadAsync(cancellationToken);
        return Ok(new { message = "The API token has been revoked" });
    }

    private async Task<string?> CheckDiscoveryAsync(string authority, CancellationToken cancellationToken)
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

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "none" ? null : value.Trim();

    private static ContentResult MessagePage(string title, string message, string? note, string actionHref, string actionText)
    {
        var encoder = HtmlEncoder.Default;
        var noteHtml = note == null ? "" : $"<p class=\"note\">{encoder.Encode(note)}</p>";
        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>Speedtest Watcher - {{encoder.Encode(title)}}</title>
            <link rel="icon" type="image/svg+xml" href="/img/logo.svg" />
            <style>
              body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #0b0f14; color: #e2e8f0;
                     font-family: Inter, -apple-system, "Segoe UI", sans-serif; }
              main { box-sizing: border-box; width: min(440px, calc(100vw - 32px)); padding: 32px; background: #151b23;
                     border: 1px solid #263238; border-radius: 10px; }
              img { width: 40px; height: 40px; }
              h1 { font-size: 20px; margin: 16px 0 8px; }
              p { color: #94a3b8; font-size: 14px; line-height: 1.5; margin: 0 0 12px; overflow-wrap: anywhere; }
              .note { font-size: 13px; }
              a { display: inline-block; margin-top: 8px; padding: 8px 16px; border-radius: 8px; background: #10b981; color: #fff;
                  text-decoration: none; font-weight: 600; font-size: 14px; }
              @media (prefers-color-scheme: light) {
                body { background: #f1f5f9; color: #1e293b; }
                main { background: #fff; border-color: #e2e8f0; }
                p { color: #64748b; }
              }
            </style>
            </head>
            <body>
            <main>
              <img src="/img/logo.svg" alt="" />
              <h1>{{encoder.Encode(title)}}</h1>
              <p>{{encoder.Encode(message)}}</p>
              {{noteHtml}}
              <a href="{{encoder.Encode(actionHref)}}">{{encoder.Encode(actionText)}}</a>
            </main>
            </body>
            </html>
            """;

        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8" };
    }
}
