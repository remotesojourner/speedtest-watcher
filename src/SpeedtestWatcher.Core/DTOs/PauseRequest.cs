namespace SpeedtestWatcher.Core.DTOs;

public class PauseRequest
{
    public const double MaxResumeInHours = 720;
    public double? ResumeIn { get; set; }
}
