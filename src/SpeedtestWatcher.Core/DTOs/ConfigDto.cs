using System.Text.Json;
using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;

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
    public SpeedtestProvider Provider => Choice<SpeedtestProvider>("provider");

    [JsonIgnore]
    public string? OoklaId => this.TryGetValue("ooklaId", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? LibreId => this.TryGetValue("libreId", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? LibreUrl => this.TryGetValue("libreUrl", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public VisitorAccess VisitorAccess => Choice<VisitorAccess>("visitorAccess");

    [JsonIgnore]
    public string? AuthEnabled => this.TryGetValue("authEnabled", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OidcAuthority => this.TryGetValue("oidcAuthority", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OidcClientId => this.TryGetValue("oidcClientId", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? OidcScopes => this.TryGetValue("oidcScopes", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public bool AuthActive => IsTrue("authActive");

    [JsonIgnore]
    public bool AuthDisabledByEnvironment => IsTrue("authDisabledByEnv");

    [JsonIgnore]
    public bool OidcClientSecretSet => IsTrue("oidcClientSecretSet");

    [JsonIgnore]
    public bool ApiTokenSet => IsTrue("apiTokenSet");

    [JsonIgnore]
    public string? Interface => this.TryGetValue("interface", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? RetentionDays => this.TryGetValue("retentionDays", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public ServerMode ServerMode => Choice<ServerMode>("serverMode");

    [JsonIgnore]
    public ServerListMode ServerListMode => Choice<ServerListMode>("serverListMode");

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

    [JsonIgnore]
    public string? ChartRange => this.TryGetValue("chartRange", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? ChartBeginAtZero => this.TryGetValue("chartBeginAtZero", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public string? DateFormat => this.TryGetValue("dateFormat", out var v) ? v?.ToString() : null;

    [JsonIgnore]
    public bool ViewMode => IsTrue("viewMode");

    private TEnum Choice<TEnum>(string key) where TEnum : struct, Enum =>
        EnumNames.TryParse<TEnum>(TryGetValue(key, out var v) ? v?.ToString() : null, out var choice) ? choice : default;

    private bool IsTrue(string key) => TryGetValue(key, out var v) && v switch
    {
        bool b => b,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        _ => false
    };
}

public class UpdateConfigKeyRequest
{
    public object? Value { get; set; }
}
