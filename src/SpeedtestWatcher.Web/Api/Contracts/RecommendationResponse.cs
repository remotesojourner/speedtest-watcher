using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Suggested targets: the best ping, download and upload of the last 10 completed tests.
/// </summary>
public sealed record RecommendationResponse
{
    /// <summary>
    /// The lowest ping, in milliseconds.
    /// </summary>
    /// <example>11</example>
    public required int Ping { get; init; }

    /// <summary>
    /// The highest download speed, in Mbps.
    /// </summary>
    /// <example>948.2</example>
    public required double Download { get; init; }

    /// <summary>
    /// The highest upload speed, in Mbps.
    /// </summary>
    /// <example>112.4</example>
    public required double Upload { get; init; }

    public static RecommendationResponse From(Recommendation recommendation) => new()
    {
        Ping = recommendation.Ping,
        Download = recommendation.Download,
        Upload = recommendation.Upload
    };
}
