using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Web.Api.Contracts;

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
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Message = OperationResult.DeniedMessage });
            return;
        }

        var returnUrl = request.PathBase + request.Path + request.QueryString;
        context.Response.Redirect($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
