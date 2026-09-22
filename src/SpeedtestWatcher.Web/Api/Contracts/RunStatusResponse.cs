using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Whether a speedtest is running and whether tests are paused.
/// </summary>
public sealed record RunStatusResponse
{
    /// <summary>
    /// A speedtest is running now.
    /// </summary>
    public required bool Running { get; init; }

    /// <summary>
    /// Scheduled and manual tests are paused.
    /// </summary>
    public required bool Paused { get; init; }

    public static RunStatusResponse From(StatusDto status) => new() { Running = status.Running, Paused = status.Paused };
}
