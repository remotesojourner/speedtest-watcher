using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class RequestAccess
{
    private readonly AuthSettings _authSettings;
    private readonly InternalAccessToken _internalToken;

    public RequestAccess(AuthSettings authSettings, InternalAccessToken internalToken)
    {
        _authSettings = authSettings;
        _internalToken = internalToken;
    }

    public Access Of(HttpContext context)
    {
        var settings = _authSettings.Current;
        return AccessPolicy.Decide(
            settings,
            signedIn: context.User.Identity?.IsAuthenticated == true,
            internalCall: _internalToken.Matches(context.Request.Headers[InternalAccessToken.HeaderName].FirstOrDefault()),
            validApiToken: ApiToken.Matches(BearerToken.FromRequest(context.Request), settings.ApiTokenHash));
    }
}
