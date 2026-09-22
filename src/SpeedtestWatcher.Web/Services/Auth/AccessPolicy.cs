using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.Services.Auth;

public static class AccessPolicy
{
    public static Access Decide(AuthSnapshot settings, bool signedIn, bool validApiToken)
    {
        if (!settings.IsActive) return Access.Full;
        if (signedIn || validApiToken) return Access.Full;
        return settings.VisitorAccess == VisitorAccess.Read ? Access.ReadOnly : Access.None;
    }

    public static bool Allows(Access access, Access required) =>
        access == Access.Full || (required == Access.ReadOnly && access == Access.ReadOnly);

    public static bool IsProgrammatic(PathString path) =>
        path.StartsWithSegments("/api") || path.StartsWithSegments("/_blazor");
}
