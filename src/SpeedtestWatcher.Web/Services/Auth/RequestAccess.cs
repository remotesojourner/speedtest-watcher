using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class RequestAccess
{
    private readonly AuthSettings _authSettings;

    public RequestAccess(AuthSettings authSettings)
    {
        _authSettings = authSettings;
    }

    public Access Of(HttpContext context)
    {
        var settings = _authSettings.Current;
        return AccessPolicy.Decide(
            settings,
            signedIn: context.User.Identity?.IsAuthenticated == true,
            validApiToken: ApiToken.Matches(BearerToken.FromRequest(context.Request), settings.ApiTokenHash));
    }
}
