using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.Settings;

public sealed record SignInSettings(
    bool Enabled,
    VisitorAccess VisitorAccess,
    string? Authority,
    string? ClientId,
    string? ClientSecret,
    IReadOnlyList<string> Scopes,
    string? ApiTokenHash)
{
    public static IReadOnlyList<string> ParseScopes(string? raw)
    {
        var scopes = (raw ?? "")
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!scopes.Contains("openid")) scopes.Insert(0, "openid");
        return scopes;
    }
}
