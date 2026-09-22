using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// The recommended targets in a backup. They're restored only when all three values are positive.
/// </summary>
public sealed record SettingsBackupRecommendation
{
    /// <summary>
    /// The stored row's id. It is ignored on import.
    /// </summary>
    /// <example>1</example>
    public int Id { get; init; }

    /// <summary>
    /// The recommended ping, in milliseconds.
    /// </summary>
    /// <example>11</example>
    public int Ping { get; init; }

    /// <summary>
    /// The recommended download speed, in Mbps.
    /// </summary>
    /// <example>948.2</example>
    public double Download { get; init; }

    /// <summary>
    /// The recommended upload speed, in Mbps.
    /// </summary>
    /// <example>112.4</example>
    public double Upload { get; init; }

    public static SettingsBackupRecommendation From(Recommendation recommendation) => new()
    {
        Id = recommendation.Id,
        Ping = recommendation.Ping,
        Download = recommendation.Download,
        Upload = recommendation.Upload
    };

    public Recommendation ToRecommendation() => new() { Id = Id, Ping = Ping, Download = Download, Upload = Upload };
}
