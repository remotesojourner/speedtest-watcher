namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Why a request failed. Every error the API answers carries this body.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// What went wrong, written for people.
    /// </summary>
    public required string Message { get; init; }
}
