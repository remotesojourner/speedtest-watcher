using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Application.Events;

public interface IAppEvents
{
    event Action? TestStarted;

    event Action<SpeedtestDto>? TestFinished;

    event Action<RunStatus>? RunStatusChanged;

    event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    void PublishTestStarted();

    void PublishTestFinished(SpeedtestDto result);

    void PublishRunStatusChanged(RunStatus status);

    void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes);
}
