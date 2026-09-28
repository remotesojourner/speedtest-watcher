namespace SpeedtestWatcher.Application.Models;

public sealed record PreTestCheck(bool Proceed, string? SkipReason, string? PublicIp = null)
{
    public static readonly PreTestCheck Ok = new(true, null);
}
