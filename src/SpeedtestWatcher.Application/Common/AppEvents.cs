using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Common;

public sealed class AppEvents : IAppEvents
{
    public event Action? TestStarted;

    public event Action<SpeedtestDto>? TestFinished;

    public event Action<RunStatus>? RunStatusChanged;

    public event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    public void PublishTestStarted() => TestStarted?.Invoke();

    public void PublishTestFinished(SpeedtestDto result) => TestFinished?.Invoke(result);

    public void PublishRunStatusChanged(RunStatus status) => RunStatusChanged?.Invoke(status);

    public void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes) => SettingsChanged?.Invoke(changes);
}
