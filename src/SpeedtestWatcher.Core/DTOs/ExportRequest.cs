namespace SpeedtestWatcher.Core.DTOs;

public class ExportRequest
{
    public string Format { get; set; } = "csv";

    public string? Status { get; set; }

    public string? Type { get; set; }

    public bool? Healthy { get; set; }

    public List<int>? Ids { get; set; }
}
