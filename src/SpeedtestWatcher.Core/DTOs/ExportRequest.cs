namespace SpeedtestWatcher.Core.DTOs;

/// <summary>Which results to export: those matching the filters, narrowed to <see cref="Ids"/> when given.</summary>
public class ExportRequest
{
    /// <summary>csv or json.</summary>
    public string Format { get; set; } = "csv";

    public string? Status { get; set; }

    public string? Type { get; set; }

    public bool? Healthy { get; set; }

    public List<int>? Ids { get; set; }
}
