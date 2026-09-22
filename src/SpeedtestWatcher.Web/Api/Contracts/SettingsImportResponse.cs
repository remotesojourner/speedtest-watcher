using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// What a settings backup import restored.
/// </summary>
public sealed record SettingsImportResponse
{
    /// <summary>
    /// Settings that were saved.
    /// </summary>
    /// <example>21</example>
    public required int Settings { get; init; }

    /// <summary>
    /// Integrations that were added or replaced.
    /// </summary>
    /// <example>1</example>
    public required int Integrations { get; init; }

    /// <summary>
    /// Whether the recommendations were restored.
    /// </summary>
    public required bool Recommendations { get; init; }

    /// <summary>
    /// Entries that were left out because they weren't valid, such as unknown settings or integration types.
    /// </summary>
    /// <example>0</example>
    public required int Skipped { get; init; }

    public static SettingsImportResponse From(SettingsImportResultDto result) => new()
    {
        Settings = result.Settings,
        Integrations = result.Integrations,
        Recommendations = result.Recommendations,
        Skipped = result.Skipped
    };
}
