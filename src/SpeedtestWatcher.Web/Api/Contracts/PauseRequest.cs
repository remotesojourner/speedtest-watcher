namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// How long to pause speedtests.
/// </summary>
public sealed record PauseRequest
{
    /// <summary>
    /// Resume automatically after this many hours, at most 720 (30 days). Leave it out to pause until tests are resumed.
    /// </summary>
    /// <example>6</example>
    public double? ResumeIn { get; init; }
}
