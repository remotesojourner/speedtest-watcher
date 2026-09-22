using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// The running version and the latest release.
/// </summary>
public sealed record VersionResponse
{
    /// <summary>
    /// The version of this instance.
    /// </summary>
    /// <example>2.0.0</example>
    public required string Local { get; init; }

    /// <summary>
    /// The latest release on GitHub, or <c>0</c> when it couldn't be checked. It is checked at most every six hours.
    /// </summary>
    /// <example>2.1.0</example>
    public required string Remote { get; init; }

    public static VersionResponse From(VersionInfoDto version) => new() { Local = version.Local, Remote = version.Remote };
}
