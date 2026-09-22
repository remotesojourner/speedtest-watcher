namespace SpeedtestWatcher.Application.SignIn;

public interface ICurrentAccess
{
    Access Level { get; }

    bool HasFullAccess => Level == Access.Full;
}
