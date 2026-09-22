namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Confirms that a request which changes something succeeded.
/// </summary>
public sealed record MessageResponse
{
    /// <summary>
    /// What happened, written for people.
    /// </summary>
    public required string Message { get; init; }
}
