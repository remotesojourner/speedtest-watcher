namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface ISignInStateService
{
    bool IsActive { get; }

    bool DisabledByEnvironment { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);
}
