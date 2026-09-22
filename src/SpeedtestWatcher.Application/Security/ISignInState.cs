namespace SpeedtestWatcher.Application.Security;

public interface ISignInState
{
    bool IsActive { get; }

    bool DisabledByEnvironment { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);
}
