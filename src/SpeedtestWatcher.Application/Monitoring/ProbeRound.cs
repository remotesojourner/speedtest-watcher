namespace SpeedtestWatcher.Application.Monitoring;

public class ProbeRound
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public bool Passed { get; set; }
    public int Answered { get; set; }
    public int Asked { get; set; }
    public double? FastestMilliseconds { get; set; }
    public bool DuringTest { get; set; }
}
