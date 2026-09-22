using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One saved setting in a backup.
/// </summary>
public sealed record SettingsBackupEntry
{
    /// <summary>
    /// The setting's name, as in <c>GET /api/config</c>.
    /// </summary>
    /// <example>cron</example>
    public string Key { get; init; } = "";

    /// <summary>
    /// The saved value. <c>none</c> means unset.
    /// </summary>
    /// <example>0 * * * *</example>
    public string Value { get; init; } = "";

    public static SettingsBackupEntry From(ConfigEntry entry) => new() { Key = entry.Key, Value = entry.Value };

    public ConfigEntry ToEntry() => new() { Key = Key, Value = Value };
}
