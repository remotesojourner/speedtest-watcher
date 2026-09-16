using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Middleware;

public class AccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthSettings _authSettings;
    private readonly InternalAccessToken _internalToken;

    public AccessMiddleware(RequestDelegate next, AuthSettings authSettings, InternalAccessToken internalToken)
    {
        _next = next;
        _authSettings = authSettings;
        _internalToken = internalToken;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (AccessPolicy.IsPublic(request.Path))
        {
            await _next(context);
            return;
        }

        var settings = _authSettings.Current;
        var access = AccessPolicy.Decide(
            settings,
            signedIn: context.User.Identity?.IsAuthenticated == true,
            internalCall: _internalToken.Matches(request.Headers[InternalAccessToken.HeaderName].FirstOrDefault()),
            validApiToken: ApiToken.Matches(ApiToken.FromRequest(request), settings.ApiTokenHash));

        if (access == Access.None)
        {
            if (AccessPolicy.IsProgrammatic(request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await context.Response.WriteAsJsonAsync(new { message = "Authentication required" });
            }
            else
            {
                var returnUrl = request.PathBase + request.Path + request.QueryString;
                context.Response.Redirect($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            return;
        }

        context.Items["ViewMode"] = access == Access.ReadOnly;
        await _next(context);
    }
}
