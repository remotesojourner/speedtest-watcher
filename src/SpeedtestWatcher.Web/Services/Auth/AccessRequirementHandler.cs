using Microsoft.AspNetCore.Authorization;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class AccessRequirementHandler : AuthorizationHandler<AccessRequirement>
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly RequestAccess _requestAccess;

    public AccessRequirementHandler(IHttpContextAccessor httpContext, RequestAccess requestAccess)
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
