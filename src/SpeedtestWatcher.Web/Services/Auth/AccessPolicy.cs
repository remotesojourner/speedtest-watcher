using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Services.Auth;

public enum Access
{
    Full,
    ReadOnly,
    None
}

public static class AccessPolicy
{
    public static Access Decide(AuthSnapshot settings, bool signedIn, bool internalCall, bool validApiToken)
    {
        if (!settings.IsActive) return Access.Full;
        if (signedIn || internalCall || validApiToken) return Access.Full;
        return settings.VisitorAccess == VisitorAccess.Read ? Access.ReadOnly : Access.None;
    }

    public static bool IsPublic(PathString path) =>
        path.StartsWithSegments("/auth") || path.StartsWithSegments("/signin-oidc");

    public static bool IsProgrammatic(PathString path) =>
        path.StartsWithSegments("/api") || path.StartsWithSegments("/_blazor") || path.StartsWithSegments(SpeedtestHub.HubUrl);
}
