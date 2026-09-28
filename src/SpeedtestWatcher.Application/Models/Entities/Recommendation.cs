namespace SpeedtestWatcher.Application.Models.Entities;

public class Recommendation
{
    public int Id { get; set; }
    public int Ping { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
}
