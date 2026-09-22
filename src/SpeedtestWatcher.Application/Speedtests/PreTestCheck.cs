namespace SpeedtestWatcher.Application.Speedtests;

public sealed record PreTestCheck(bool Proceed, string? SkipReason)
{
    public static readonly PreTestCheck Ok = new(true, null);
}
