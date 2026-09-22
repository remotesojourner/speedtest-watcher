using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// What a results import stored.
/// </summary>
public sealed record TestImportResponse
{
    /// <summary>
    /// Results that were stored.
    /// </summary>
    /// <example>40</example>
    public required int Imported { get; init; }

    /// <summary>
    /// Results that were left out because a result with the same timestamp is already stored.
    /// </summary>
    /// <example>1</example>
    public required int Skipped { get; init; }

    public static TestImportResponse From(TestImportResultDto result) => new() { Imported = result.Imported, Skipped = result.Skipped };
}
