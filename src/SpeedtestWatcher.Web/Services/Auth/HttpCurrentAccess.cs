using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class HttpCurrentAccess : ICurrentAccess
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly RequestAccess _requestAccess;

    public HttpCurrentAccess(IHttpContextAccessor httpContext, RequestAccess requestAccess)
    {
        _httpContext = httpContext;
        _requestAccess = requestAccess;
    }

    public Access Level => _httpContext.HttpContext is { } context ? _requestAccess.Of(context) : Access.None;
}
