using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.DTOs;

public class ExportRequest
{
    public string Format { get; set; } = "csv";

    public TestStatus? Status { get; set; }

    public TestType? Type { get; set; }

    public bool? Healthy { get; set; }

    public List<int>? Ids { get; set; }
}
