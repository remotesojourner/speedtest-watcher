namespace SpeedtestWatcher.Core.Models;

public class Speedtest
{
    public int Id { get; set; }
    public int ServerId { get; set; } = 0;
    public string? ServerName { get; set; }
    public string? ServerHost { get; set; }
    public int Ping { get; set; }
    public double? Jitter { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
    public string? Error { get; set; }

    /// <summary>completed, failed or skipped.</summary>
    public string Status { get; set; } = "completed";

    /// <summary>Whether the result met the thresholds below; null when it wasn't measured.</summary>
    public bool? Healthy { get; set; }

    // The thresholds in force when the test ran, so changing your targets later doesn't rewrite history.
    public int? ThresholdPing { get; set; }
    public double? ThresholdDownload { get; set; }
    public double? ThresholdUpload { get; set; }

    public string Type { get; set; } = "auto";
    public string? ResultId { get; set; }
    public int Time { get; set; } = 0;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
