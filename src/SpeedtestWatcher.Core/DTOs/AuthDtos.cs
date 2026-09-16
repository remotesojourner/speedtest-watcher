namespace SpeedtestWatcher.Core.DTOs;

/// <summary>The Security tab's settings, saved together so sign-in is never switched on half-configured.</summary>
public class AuthSettingsRequest
{
    public bool Enabled { get; set; }

    /// <summary>none or read.</summary>
    public string VisitorAccess { get; set; } = "none";

    /// <summary>The provider's issuer URL; its discovery document is read from here.</summary>
    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    /// <summary>A new client secret. Leave empty to keep the saved one.</summary>
    public string? ClientSecret { get; set; }

    public bool ClearClientSecret { get; set; }

    public string? Scopes { get; set; }
}

public class ApiTokenResponse
{
    public string Token { get; set; } = "";
}