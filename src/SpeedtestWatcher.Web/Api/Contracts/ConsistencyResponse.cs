using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// How steady the readings of the completed tests were. All values are <c>0</c> when no test completed.
/// </summary>
public sealed record ConsistencyResponse
{
    /// <summary>
    /// Download speed consistency.
    /// </summary>
    public required SpeedConsistencyResponse Download { get; init; }

    /// <summary>
    /// Upload speed consistency.
    /// </summary>
    public required SpeedConsistencyResponse Upload { get; init; }

    /// <summary>
    /// Ping consistency.
    /// </summary>
    public required PingConsistencyResponse Ping { get; init; }

    public static ConsistencyResponse From(ConsistencyDto consistency) => new()
    {
        Download = SpeedConsistencyResponse.From(consistency.Download),
        Upload = SpeedConsistencyResponse.From(consistency.Upload),
        Ping = new PingConsistencyResponse { StdDev = consistency.Ping.StdDev, Jitter = consistency.Ping.Jitter }
    };
}
