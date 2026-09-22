using SpeedtestWatcher.Application.Integrations;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One configured integration in a backup.
/// </summary>
public sealed record SettingsBackupIntegration
{
    /// <summary>
    /// The integration's id. An import replaces the integration with the same id.
    /// </summary>
    /// <example>3f9c2a1b7d4e</example>
    public string Id { get; init; } = "";

    /// <summary>
    /// The name shown on the Integrations tab.
    /// </summary>
    /// <example>Home hook</example>
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// The integration type, such as <c>webhook</c>, <c>discord</c> or <c>influxdb</c>.
    /// </summary>
    /// <example>webhook</example>
    public string Name { get; init; } = "";

    /// <summary>
    /// The integration's settings, as a JSON object written out as a string.
    /// </summary>
    /// <example>{"url":"https://example.com/hook","send_finished":true}</example>
    public string Data { get; init; } = "{}";

    /// <summary>
    /// When the integration last sent something, in UTC.
    /// </summary>
    public DateTime? LastActivity { get; init; }

    /// <summary>
    /// Whether the last send failed.
    /// </summary>
    public bool ActivityFailed { get; init; }

    public static SettingsBackupIntegration From(IntegrationData integration) => new()
    {
        Id = integration.Id,
        DisplayName = integration.DisplayName,
        Name = integration.Name,
        Data = integration.Data,
        LastActivity = integration.LastActivity,
        ActivityFailed = integration.ActivityFailed
    };

    public IntegrationData ToData() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Name = Name,
        Data = Data,
        LastActivity = LastActivity,
        ActivityFailed = ActivityFailed
    };
}
