using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Web.Services;

public sealed class HttpCurrentAccessService : ICurrentAccessService
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly RequestAccessService _requestAccess;

    public HttpCurrentAccessService(IHttpContextAccessor httpContext, RequestAccessService requestAccess)
    {
        _httpContext = httpContext;
        _requestAccess = requestAccess;
    }

    public Access Level => _httpContext.HttpContext is { } context ? _requestAccess.Of(context) : Access.None;
}
