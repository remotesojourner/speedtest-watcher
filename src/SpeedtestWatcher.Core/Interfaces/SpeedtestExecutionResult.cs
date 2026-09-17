namespace SpeedtestWatcher.Core.Interfaces;

public class SpeedtestExecutionResult
{
    public bool Success { get; set; }

    public bool Skipped { get; set; }
    public int Ping { get; set; }
    public double? Jitter { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
    public int Time { get; set; }
    public int ServerId { get; set; }
    public string? ServerName { get; set; }
    public string? ServerHost { get; set; }
    public string? ResultId { get; set; }
    public string? Error { get; set; }
}
