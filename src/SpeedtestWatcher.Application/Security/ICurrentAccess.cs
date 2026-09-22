namespace SpeedtestWatcher.Application.Security;

public interface ICurrentAccess
{
    Access Level { get; }

    bool HasFullAccess => Level == Access.Full;
}
