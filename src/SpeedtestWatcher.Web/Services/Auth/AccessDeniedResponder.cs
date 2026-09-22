using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SpeedtestWatcher.Application;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class AccessDeniedResponder : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        var request = context.Request;
        if (AccessPolicy.IsProgrammatic(request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new { message = OperationResult.DeniedMessage });
            return;
        }

        var returnUrl = request.PathBase + request.Path + request.QueryString;
        context.Response.Redirect($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
