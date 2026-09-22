using System.Security.Claims;

namespace SpeedtestWatcher.Web.SignIn;

public static class SignedInUser
{
    public static string DisplayName(ClaimsPrincipal user) =>
        user.FindFirst("name")?.Value
        ?? user.FindFirst("preferred_username")?.Value
        ?? user.FindFirst("email")?.Value
        ?? user.FindFirst("sub")?.Value
        ?? "you";
}
