using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// A settings backup: the settings, integrations and recommendations, in the same format as the file the Storage tab exports. Sign-in settings are never included.
/// </summary>
public sealed record SettingsBackup
{
    /// <summary>
    /// The saved settings.
    /// </summary>
    public IReadOnlyList<SettingsBackupEntry> Config { get; init; } = [];

    /// <summary>
    /// The configured integrations.
    /// </summary>
    public IReadOnlyList<SettingsBackupIntegration> Integrations { get; init; } = [];

    /// <summary>
    /// The recommended targets, or <c>null</c> before 10 tests have completed.
    /// </summary>
    public SettingsBackupRecommendation? Recommendations { get; init; }

    public static SettingsBackup From(SettingsBackupDto backup) => new()
    {
        Config = backup.Config.Select(SettingsBackupEntry.From).ToList(),
        Integrations = backup.Integrations.Select(SettingsBackupIntegration.From).ToList(),
        Recommendations = backup.Recommendations is { } recommendations ? SettingsBackupRecommendation.From(recommendations) : null
    };

    public SettingsBackupDto ToDto() => new()
    {
        Config = Config.Select(entry => entry.ToEntry()).ToList(),
        Integrations = Integrations.Select(integration => integration.ToData()).ToList(),
        Recommendations = Recommendations?.ToRecommendation()
    };
}
