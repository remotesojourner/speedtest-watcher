namespace SpeedtestWatcher.Core.Models;

public class Speedtest
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

    public string Status { get; set; } = "completed";

    public bool? Healthy { get; set; }

    public int? ThresholdPing { get; set; }
    public double? ThresholdDownload { get; set; }
    public double? ThresholdUpload { get; set; }

    public string Type { get; set; } = "auto";
    public string? ResultId { get; set; }
    public int Time { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
