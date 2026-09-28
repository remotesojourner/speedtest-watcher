using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Web.Models;

public sealed record AuthSnapshot(
    bool Enabled,
    VisitorAccess VisitorAccess,
    string? Authority,
    string? ClientId,
    string? ClientSecret,
    IReadOnlyList<string> Scopes,
    string? ApiTokenHash,
    bool DisabledByEnvironment)
{
    public static readonly AuthSnapshot Default = new(false, VisitorAccess.None, null, null, null, ["openid", "profile", "email"], null, false);

    public bool Configured => !string.IsNullOrEmpty(Authority) && !string.IsNullOrEmpty(ClientId);

    public bool IsActive => Enabled && Configured && !DisabledByEnvironment;
}
