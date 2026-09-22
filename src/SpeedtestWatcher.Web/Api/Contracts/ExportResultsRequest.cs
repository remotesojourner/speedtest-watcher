using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Which results to export and in what format. Leave everything out to export every result as CSV.
/// </summary>
public sealed record ExportResultsRequest
{
    /// <summary>
    /// <c>csv</c> or <c>json</c>. Anything else gives CSV. The JSON can be imported again.
    /// </summary>
    /// <example>json</example>
    public string Format { get; init; } = "csv";

    /// <summary>
    /// Only results with this status.
    /// </summary>
    public TestStatus? Status { get; init; }

    /// <summary>
    /// Only results started this way.
    /// </summary>
    public TestType? Type { get; init; }

    /// <summary>
    /// Only results that met (<c>true</c>) or missed (<c>false</c>) their targets.
    /// </summary>
    public bool? Healthy { get; init; }

    /// <summary>
    /// Only these results. The other filters still apply.
    /// </summary>
    /// <example>[41, 42]</example>
    public IReadOnlyList<int>? Ids { get; init; }

    public ExportRequest ToExportRequest() => new()
    {
        Format = Format,
        Status = Status,
        Type = Type,
        Healthy = Healthy,
        Ids = Ids?.ToList()
    };
}
