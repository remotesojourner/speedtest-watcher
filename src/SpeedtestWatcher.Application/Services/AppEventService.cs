using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed class AppEventService : IAppEventService
{
    public event Action? TestStarted;

    public event Action<SpeedtestDto>? TestFinished;

    public event Action<RunStatus>? RunStatusChanged;

    public event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    public event Action<ConnectionSnapshot>? ConnectionChanged;

    public void PublishTestStarted() => TestStarted?.Invoke();

    public void PublishTestFinished(SpeedtestDto result) => TestFinished?.Invoke(result);

    public void PublishRunStatusChanged(RunStatus status) => RunStatusChanged?.Invoke(status);

    public void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes) => SettingsChanged?.Invoke(changes);

    public void PublishConnectionChanged(ConnectionSnapshot connection) => ConnectionChanged?.Invoke(connection);
}
