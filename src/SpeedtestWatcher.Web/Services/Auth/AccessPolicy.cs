using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Services.Auth;

public enum Access
{
    Full,
    ReadOnly,
    None
}

/// <summary>Decides what a request may do. Kept apart from the middleware so the rules can be tested on their own.</summary>
public static class AccessPolicy
{
    public static Access Decide(AuthSnapshot settings, bool signedIn, bool internalCall, bool validApiToken)
    {
        if (!settings.IsActive) return Access.Full;
        if (signedIn || internalCall || validApiToken) return Access.Full;
        return settings.VisitorAccess == "read" ? Access.ReadOnly : Access.None;
    }

    /// <summary>Signing in, and seeing why it failed, has to work for someone who isn't signed in yet.</summary>
    public static bool IsPublic(PathString path) =>
        path.StartsWithSegments("/auth") || path.StartsWithSegments("/signin-oidc");

    /// <summary>Requests made by programs rather than a person at a page: refused with a 401, not sent to a sign-in page.</summary>
    public static bool IsProgrammatic(PathString path) =>
        path.StartsWithSegments("/api") || path.StartsWithSegments("/_blazor") || path.StartsWithSegments(SpeedtestHub.HubUrl);
}
