using Microsoft.AspNetCore.Authorization;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Utils;

namespace SpeedtestWatcher.Web.Authorization;

public sealed class AccessRequirementHandler : AuthorizationHandler<AccessRequirement>
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly RequestAccessService _requestAccess;

    public AccessRequirementHandler(IHttpContextAccessor httpContext, RequestAccessService requestAccess)
    {
        _httpContext = httpContext;
        _requestAccess = requestAccess;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessRequirement requirement)
    {
        if (_httpContext.HttpContext is { } request && AccessPolicy.Allows(_requestAccess.Of(request), requirement.Required))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
