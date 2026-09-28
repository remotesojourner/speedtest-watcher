namespace SpeedtestWatcher.Application.Models.Entities;

public class Outage
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public TimeSpan? Length => EndedAt - StartedAt;
}
