using System.Security.Claims;

namespace SpeedtestWatcher.Web.Services.Auth;

public static class SignedInUser
{
    /// <summary>Providers differ in which claims they send, so this falls back through the usual ones.</summary>
    public static string DisplayName(ClaimsPrincipal user) =>
        user.FindFirst("name")?.Value
        ?? user.FindFirst("preferred_username")?.Value
        ?? user.FindFirst("email")?.Value
        ?? user.FindFirst("sub")?.Value
        ?? "you";
}
