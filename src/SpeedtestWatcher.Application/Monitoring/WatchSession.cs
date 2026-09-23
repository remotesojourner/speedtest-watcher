namespace SpeedtestWatcher.Application.Monitoring;

public class WatchSession
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
