using System.Text.Json;
using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// The saved settings, one string property per setting, plus what the caller may see and do. Unset values are <c>none</c>. Read-only visitors don't get the settings marked full access only, and secrets are never returned.
/// </summary>
public sealed class ConfigResponse
{
    private static readonly string[] _flags = ["viewMode", "authActive", "authDisabledByEnv", "oidcClientSecretSet", "apiTokenSet"];

    /// <summary>
    /// The caller has read-only access.
    /// </summary>
    public required bool ViewMode { get; init; }

    /// <summary>
    /// Sign-in is switched on and enforced.
    /// </summary>
    public required bool AuthActive { get; init; }

    /// <summary>
    /// Sign-in is switched off by the <c>DISABLE_AUTH</c> environment variable.
    /// </summary>
    public required bool AuthDisabledByEnv { get; init; }

    /// <summary>
    /// A client secret is saved for the identity provider. Left out for read-only visitors.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OidcClientSecretSet { get; init; }

    /// <summary>
    /// An API token exists. Left out for read-only visitors.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ApiTokenSet { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Settings { get; init; } = [];

    public static ConfigResponse From(ConfigDto config) => new()
    {
        ViewMode = Flag(config, "viewMode") ?? false,
        AuthActive = Flag(config, "authActive") ?? false,
        AuthDisabledByEnv = Flag(config, "authDisabledByEnv") ?? false,
        OidcClientSecretSet = Flag(config, "oidcClientSecretSet"),
        ApiTokenSet = Flag(config, "apiTokenSet"),
        Settings = config
            .Where(entry => !_flags.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => JsonSerializer.SerializeToElement(entry.Value?.ToString()))
    };

    private static bool? Flag(ConfigDto config, string key) => config.TryGetValue(key, out var value) && value is bool flag ? flag : null;
}
