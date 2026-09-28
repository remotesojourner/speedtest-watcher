namespace SpeedtestWatcher.Application.Models;

public sealed record ProbeResult(int Answered, int Asked, double? FastestMilliseconds)
{
    public bool Passed => Asked > 0 && Answered * 2 > Asked;
}
