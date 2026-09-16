namespace SpeedtestWatcher.Core.DTOs;

public class AuthSettingsRequest
{
    public bool Enabled { get; set; }

    public string VisitorAccess { get; set; } = "none";

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool ClearClientSecret { get; set; }

    public string? Scopes { get; set; }
}

public class ApiTokenResponse
{
    public string Token { get; set; } = "";
}