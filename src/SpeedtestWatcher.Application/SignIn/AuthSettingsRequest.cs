namespace SpeedtestWatcher.Application.SignIn;

public class AuthSettingsRequest
{
    public bool Enabled { get; set; }

    public VisitorAccess VisitorAccess { get; set; } = VisitorAccess.None;

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool ClearClientSecret { get; set; }

    public string? Scopes { get; set; }
}
