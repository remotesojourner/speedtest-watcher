using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Utils;
using SpeedtestWatcher.Web.Utils;

namespace SpeedtestWatcher.Web.Services;

public sealed class RequestAccessService
{
    private readonly AuthSettingsService _authSettings;

    public RequestAccessService(AuthSettingsService authSettings)
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
