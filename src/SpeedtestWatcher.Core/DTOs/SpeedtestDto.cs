using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.DTOs;

public class SpeedtestDto
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string? ServerName { get; set; }
    public string? ServerHost { get; set; }
    public int Ping { get; set; }
    public double? Jitter { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
    public string? Error { get; set; }
    public TestStatus Status { get; set; } = TestStatus.Completed;
    public bool? Healthy { get; set; }
    public int? ThresholdPing { get; set; }
    public double? ThresholdDownload { get; set; }
    public double? ThresholdUpload { get; set; }
    public TestType Type { get; set; } = TestType.Auto;
    public string? ResultId { get; set; }
    public int Time { get; set; }
    public string Created { get; set; } = string.Empty;

    public static SpeedtestDto From(Speedtest test) => new()
    {
        Id = test.Id,
        ServerId = test.ServerId,
        ServerName = test.ServerName,
        ServerHost = test.ServerHost,
        Ping = test.Ping,
        Jitter = test.Jitter,
        Download = test.Download,
        Upload = test.Upload,
        Error = test.Error,
        Status = test.Status,
        Healthy = test.Healthy,
        ThresholdPing = test.ThresholdPing,
        ThresholdDownload = test.ThresholdDownload,
        ThresholdUpload = test.ThresholdUpload,
        Type = test.Type,
        ResultId = test.ResultId,
        Time = test.Time,
        Created = test.Created.ToString("o")
    };
}
