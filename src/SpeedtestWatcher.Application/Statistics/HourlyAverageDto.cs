namespace SpeedtestWatcher.Application.Statistics;

public class HourlyAverageDto
{
    public int Hour { get; set; }
    public double? Download { get; set; }
    public double? Upload { get; set; }
    public int? Ping { get; set; }
    public double? Jitter { get; set; }
    public int Count { get; set; }
}
