using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeedtestWatcher.Core.DTOs;

public class ConfigDto : Dictionary<string, object?>
{
    [JsonIgnore]
    public string? Ping => this.TryGetValue("ping", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? Download => this.TryGetValue("download", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? Upload => this.TryGetValue("upload", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? Cron => this.TryGetValue("cron", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? ScheduleOffset => this.TryGetValue("scheduleOffset", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? Provider => this.TryGetValue("provider", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OoklaId => this.TryGetValue("ooklaId", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? LibreId => this.TryGetValue("libreId", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? LibreUrl => this.TryGetValue("libreUrl", out var v) ? v?.ToString() : null;

    /// <summary>none or read: what someone who isn't signed in may do while sign-in is on.</summary>
    [JsonIgnore]
    public string? VisitorAccess => this.TryGetValue("visitorAccess", out var v) ? v?.ToString() : null;

    /// <summary>The saved sign-in switch. Whether sign-in is actually enforced is <see cref="AuthActive"/>.</summary>
    [JsonIgnore]
    public string? AuthEnabled => this.TryGetValue("authEnabled", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OidcAuthority => this.TryGetValue("oidcAuthority", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OidcClientId => this.TryGetValue("oidcClientId", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OidcScopes => this.TryGetValue("oidcScopes", out var v) ? v?.ToString() : null;

    /// <summary>Sign-in is being enforced right now: switched on, configured, and not overridden by DISABLE_AUTH.</summary>
    [JsonIgnore]
    public bool AuthActive => IsTrue("authActive");

    [JsonIgnore]
    public bool AuthDisabledByEnvironment => IsTrue("authDisabledByEnv");

    // Secrets are never sent to the browser; these only say whether one is stored.
    [JsonIgnore]
    public bool OidcClientSecretSet => IsTrue("oidcClientSecretSet");

    [JsonIgnore]
    public bool ApiTokenSet => IsTrue("apiTokenSet");

    [JsonIgnore]
    public string? Interface => this.TryGetValue("interface", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? RetentionDays => this.TryGetValue("retentionDays", out var v) ? v?.ToString() : null;

    /// <summary>auto, random or single.</summary>
    [JsonIgnore]
    public string? ServerMode => this.TryGetValue("serverMode", out var v) ? v?.ToString() : null;

    /// <summary>allow (test only the listed servers) or deny (test anything but them).</summary>
    [JsonIgnore]
    public string? ServerListMode => this.TryGetValue("serverListMode", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OoklaServerIds => this.TryGetValue("ooklaServerIds", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? LibreServerIds => this.TryGetValue("libreServerIds", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? InternetCheckEnabled => this.TryGetValue("internetCheckEnabled", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? InternetCheckUrl => this.TryGetValue("internetCheckUrl", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? SkipIps => this.TryGetValue("skipIps", out var v) ? v?.ToString() : null;

    /// <summary>24h, 7d or 30d: the range the dashboard opens with.</summary>
    [JsonIgnore]
    public string? ChartRange => this.TryGetValue("chartRange", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? ChartBeginAtZero => this.TryGetValue("chartBeginAtZero", out var v) ? v?.ToString() : null;

    /// <summary>dmy, mdy or ymd.</summary>
    [JsonIgnore]
    public string? DateFormat => this.TryGetValue("dateFormat", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public bool ViewMode => IsTrue("viewMode");

    [JsonIgnore]
    public bool PreviewMode => IsTrue("previewMode");

    [JsonIgnore]
    public string? PreviewMessage => this.TryGetValue("previewMessage", out var v) ? v?.ToString() : null;

    // Values deserialized from JSON arrive as JsonElement, not bool.
    private bool IsTrue(string key) => TryGetValue(key, out var v) && v switch
    {
        bool b => b,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        _ => false
    };
}

public class UpdateConfigKeyRequest
{
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}
