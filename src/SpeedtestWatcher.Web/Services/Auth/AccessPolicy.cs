using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Services.Auth;

public static class AccessPolicy
{
    public static Access Decide(AuthSnapshot settings, bool signedIn, bool internalCall, bool validApiToken)
    {
        if (!settings.IsActive) return Access.Full;
        if (signedIn || internalCall || validApiToken) return Access.Full;
        return settings.VisitorAccess == VisitorAccess.Read ? Access.ReadOnly : Access.None;
    }

    public static bool Allows(Access access, Access required) =>
        access == Access.Full || (required == Access.ReadOnly && access == Access.ReadOnly);

    public static bool IsProgrammatic(PathString path) =>
        path.StartsWithSegments("/api") || path.StartsWithSegments("/_blazor") || path.StartsWithSegments(SpeedtestHub.HubUrl);
}
